
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Velopack.Packaging.Abstractions;

#nullable enable
namespace Velopack.Packaging.Auth;

public class AuthenticationClient(VelopackServiceClient client, IConsole console) : IAuthenticationClient
{
    private static readonly string[] Scopes = ["openid", "offline_access"];

    public async Task<bool> LoginAsync(VelopackServiceOptions options)
    {
        console.WriteLine("Preparing to login to Velopack");

        AuthConfiguration authConfiguration = await client.GetAuthConfigurationAsync(options);

        IPublicClientApplication pca = await BuildPublicApplicationAsync(authConfiguration);

        var rv =
            await AcquireSilentlyAsync(pca) ??
            await AcquireInteractiveAsync(pca, authConfiguration) ??
            await AcquireByDeviceCodeAsync(pca);

        if (rv != null) {
            client.WithAuthentication(new("Bearer", rv.IdToken ?? rv.AccessToken));
            var profile = await client.GetProfileAsync(options);

            console.WriteLine($"{profile?.Name} ({profile?.Email}) logged in to Velopack");
            return true;
        } else {
            console.WriteLine("Failed to login to Velopack");
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
        console.WriteLine("Cleared saved login(s) for Velopack");
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
                    // This will print the message on the logger which tells the user where to go sign-in using 
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
        var userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var vpkPath = Path.Combine(userPath, ".vpk");

        var storageProperties =
             new StorageCreationPropertiesBuilder("creds.bin", vpkPath)
             .WithLinuxKeyring(
                 schemaName: "com.velopack.app",
                 collection: "default",
                 secretLabel: "Credentials for Velopack's VPK tool",
                 new KeyValuePair<string, string>("vpk.client-id", authConfiguration.ClientId ?? ""),
                 new KeyValuePair<string, string>("vpk.version", "v1")
              )
             .WithMacKeyChain(
                 serviceName: "velopack",
                 accountName: "vpk")
             .Build();

        var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
        cacheHelper.RegisterCache(pca.UserTokenCache);

        return pca;
    }
}
