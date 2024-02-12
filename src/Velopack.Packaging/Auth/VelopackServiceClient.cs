using System.Net.Http.Headers;

#if NET6_0_OR_GREATER
using System.Net.Http.Json;
#endif

#nullable enable
namespace Velopack.Packaging.Auth;

public class VelopackServiceClient
{
    private HttpClient HttpClient { get; }
    private AuthConfiguration? AuthConfiguration { get; set; }

    public VelopackServiceClient(HttpClient httpClient)
    {
        HttpClient = httpClient;
    }

    public void WithAuthentication(AuthenticationHeaderValue authenticationHeader)
    {
        HttpClient.DefaultRequestHeaders.Authorization = authenticationHeader;
    }

    public async Task<AuthConfiguration> GetAuthConfigurationAsync(VelopackServiceOptions options)
    {
        if (AuthConfiguration is not null)
            return AuthConfiguration;

        var endpoint = GetEndpoint("/api/v1/auth/config", options);

        var authConfig = await HttpClient.GetFromJsonAsync<AuthConfiguration>(endpoint);
        if (authConfig is null)
            throw new Exception("Failed to get auth configuration.");
        if (authConfig.B2CAuthority is null)
            throw new Exception("B2C Authority not provided.");
        if (authConfig.RedirectUri is null)
            throw new Exception("Redirect URI not provided.");
        if (authConfig.ClientId is null)
            throw new Exception("Client ID not provided.");

        return authConfig;
    }

    public async Task<Profile?> GetProfileAsync(VelopackServiceOptions options)
    {
        var endpoint = GetEndpoint("/api/v1/user/profile", options);

        return await HttpClient.GetFromJsonAsync<Profile>(endpoint);
    }

    private static Uri GetEndpoint(string relativePath, VelopackServiceOptions options)
    {
        var endpoint = new Uri(relativePath, UriKind.Relative);
        if (options.VelopackBaseUrl is { } baseUrl) {
            endpoint = new(new Uri(baseUrl), endpoint);
        }
        return endpoint;
    }
}
