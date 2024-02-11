using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Velopack.Packaging.Abstractions;
using Velopack.Vpk.Commands;

namespace Velopack.Vpk.Auth;
#nullable enable

internal class AuthenticationClient(VelopackServiceClient client, IFancyConsole console) : IAuthenticationClient
{
    private static readonly string[] Scopes = ["openid", "offline_access"];

    public async Task<bool> LoginAsync(VelopackServiceOptions options)
    {
        console.WriteLine("Preparing to login to Vellopack");

        AuthConfiguration authConfiguration = await client.GetAuthConfigurationAsync(options);

        IPublicClientApplication pca = await BuildPublicApplicationAsync(authConfiguration);

        AuthenticationResult? rv = 
            await AcquireSilentlyAsync(pca) ??
            await AcquireInteractiveAsync(pca, authConfiguration) ?? 
            await AcquireByDeviceCodeAsync(pca);

        if (rv != null) {
            client.WithAuthentication(new("Bearer", rv.IdToken ?? rv.AccessToken));
            var profile = await client.GetProfileAsync(options);

            console.WriteLine($"{profile?.Name} ({profile?.Email}) logged in to Vellopack");
            return true;
        } else {
            console.WriteLine("Failed to login to Vellopack");
            return false;
        }
    }

    public async Task LogoutAsync(VelopackServiceOptions options)
    {
        AuthConfiguration authConfiguration = await client.GetAuthConfigurationAsync(options);

        IPublicClientApplication pca = await BuildPublicApplicationAsync(authConfiguration);

        // clear the cache
        while ((await pca.GetAccountsAsync()).FirstOrDefault() is { } account) {
            await pca.RemoveAsync(account);
            console.WriteLine($"Logged out of {account.Username}");
        }
        console.WriteLine("Cleared saved login(s) for Vellopack");
    }

    private static async Task<AuthenticationResult?> AcquireSilentlyAsync(IPublicClientApplication pca)
    {
        try {
            var account = (await pca.GetAccountsAsync()).FirstOrDefault();

            if (account is not null) {
                return await pca.AcquireTokenSilent(Scopes, account)
                    .ExecuteAsync();
            }
        } catch (MsalException) {
            // No token found in the cache or Azure AD insists that a form interactive auth is required (e.g. the tenant admin turned on MFA)
        }
        return null;
    }

    private static async Task<AuthenticationResult?> AcquireInteractiveAsync(IPublicClientApplication pca, AuthConfiguration authConfiguration)
    {
        try {
            return await pca.AcquireTokenInteractive(Scopes)
                        .WithB2CAuthority(authConfiguration.B2CAuthority)
                        .ExecuteAsync();
        } catch (MsalException) {
        }
        return null;
    }

    private async Task<AuthenticationResult?> AcquireByDeviceCodeAsync(IPublicClientApplication pca)
    {
        try {
            var result = await pca.AcquireTokenWithDeviceCode(Scopes,
                deviceCodeResult => {
                    // This will print the message on the console which tells the user where to go sign-in using 
                    // a separate browser and the code to enter once they sign in.
                    // The AcquireTokenWithDeviceCode() method will poll the server after firing this
                    // device code callback to look for the successful login of the user via that browser.
                    // This background polling (whose interval and timeout data is also provided as fields in the 
                    // deviceCodeCallback class) will occur until:
                    // * The user has successfully logged in via browser and entered the proper code
                    // * The timeout specified by the server for the lifetime of this code (typically ~15 minutes) has been reached
                    // * The developing application calls the Cancel() method on a CancellationToken sent into the method.
                    //   If this occurs, an OperationCanceledException will be thrown (see catch below for more details).
                    console.WriteLine(deviceCodeResult.Message);
                    return Task.FromResult(0);
                }).ExecuteAsync();

            console.WriteLine(result.Account.Username);
            return result;
        } catch (MsalException) {
        }
        return null;
    }

    private static async Task<IPublicClientApplication> BuildPublicApplicationAsync(AuthConfiguration authConfiguration)
    {
        IPublicClientApplication pca = PublicClientApplicationBuilder
                .Create(authConfiguration.ClientId)
                .WithB2CAuthority(authConfiguration.B2CAuthority)
                .WithRedirectUri(authConfiguration.RedirectUri)
                //.WithLogging((Microsoft.Identity.Client.LogLevel level, string message, bool containsPii) => System.Console.WriteLine($"[{level}]: {message}"))
                .Build();

        //https://learn.microsoft.com/entra/msal/dotnet/how-to/token-cache-serialization?tabs=desktop&WT.mc_id=DT-MVP-5003472
        string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string vpkPath = Path.Combine(userPath, ".vpk");

        var storageProperties =
             new StorageCreationPropertiesBuilder("creds.bin", vpkPath)
             .WithLinuxKeyring(
                 schemaName: "com.vellopack.app",
                 collection: "default",
                 secretLabel: "Credentials for Vellopack's VPK tool",
                 new KeyValuePair<string, string>("vpk.client-id", authConfiguration.ClientId ?? ""),
                 new KeyValuePair<string, string>("vpk.version", "v1")
              )
             .WithMacKeyChain(
                 serviceName: "vellopack",
                 accountName: "vpk")
             .Build();

        var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
        cacheHelper.RegisterCache(pca.UserTokenCache);

        return pca;
    }
}
