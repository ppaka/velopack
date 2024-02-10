using System.Text.Json.Serialization;
using System.Net.Http;
using Velopack.Packaging.Abstractions;
using System.Net.Http.Json;

namespace Velopack.Packaging.Commands;
#nullable enable

public class LoginCommandRunner : ICommand<LoginOptions>
{
    private static HttpClient Client { get; } = new HttpClient();
    private IFancyConsole Console { get; }

    public LoginCommandRunner(IFancyConsole console)
    {
        Console = console;
    }

    public async Task Run(LoginOptions options)
    {
        Console.WriteLine("Preparing to login to Vellopack");
        
        AuthConfiguration authConfiguration = await GetAuthConfiguration(Client, options);

        var response = await StartDeviceFlowAsync(Client, authConfiguration);
        if (response is null)
            throw new Exception("Failed to start device flow.");

        Console.WriteLine($"To sign in, use a web browser to open the page {response.VerificationUri} and enter the code {response.UserCode}.");

        var token = await PollForTokenAsync(Client, authConfiguration, response);

        //TODO: Need to save this data
        Console.WriteLine($"Access token: {token?.AccessToken}");
    }

    private static async Task<AuthConfiguration> GetAuthConfiguration(HttpClient client, LoginOptions loginOptions)
    {
        Uri authConfigurationEndpoint = new(new Uri(loginOptions.VelopackBaseUrl, UriKind.Absolute), "/api/v1/auth/config");
        var authConfig = await client.GetFromJsonAsync<AuthConfiguration>(authConfigurationEndpoint);
        if (authConfig is null)
            throw new Exception("Failed to get auth configuration.");
        if (authConfig.TokenEndpoint is null)
            throw new Exception("Token endpoint not provided.");
        if (authConfig.DeviceEndpoint is null)
            throw new Exception("Device endpoint not provided.");
        if (authConfig.ClientId is null)
            throw new Exception("Client ID not provided.");

        return authConfig;
    }

    private static async Task<DeviceAuthorizationResponse?> StartDeviceFlowAsync(
        HttpClient client,
        AuthConfiguration authConfiguration)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, authConfiguration.DeviceEndpoint) {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> {
                ["client_id"] = authConfiguration.ClientId!,
                ["scope"] = "openid profile offline_access"
            })
        };
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DeviceAuthorizationResponse>();
    }

    private static async Task<TokenResponse?> PollForTokenAsync(
        HttpClient client,
        AuthConfiguration authConfiguration,
        DeviceAuthorizationResponse authResponse)
    {
        // Poll until we get a valid token response or a fatal error
        int pollingDelay = authResponse.Interval ?? 10;
        while (true) {
            var request = new HttpRequestMessage(HttpMethod.Post, authConfiguration.TokenEndpoint) {
                Content = new FormUrlEncodedContent(new Dictionary<string, string> {
                    ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
                    ["device_code"] = authResponse.DeviceCode ?? "",
                    ["client_id"] = authConfiguration.ClientId!
                })
            };
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode) {
                return await response.Content.ReadFromJsonAsync<TokenResponse>();
            } else {
                var errorResponse = await response.Content.ReadFromJsonAsync<TokenErrorResponse>();
                switch (errorResponse?.Error) {
                case "authorization_pending":
                    // Not complete yet, wait and try again later
                    break;
                case "slow_down":
                    // Not complete yet, and we should slow down the polling
                    pollingDelay += 5;
                    break;
                default:
                    // Some other error, nothing we can do but throw
                    throw new Exception(
                        $"Authorization failed: {errorResponse?.Error} - {errorResponse?.ErrorDescription}");
                }

                await Task.Delay(TimeSpan.FromSeconds(pollingDelay));
            }
        }
    }

    private class AuthConfiguration
    {
        public Uri? DeviceEndpoint { get; init; }
        public Uri? TokenEndpoint { get; init; }
        public string? ClientId { get; init; }
    }

    private class DeviceAuthorizationResponse
    {
        [JsonPropertyName("device_code")]
        public string? DeviceCode { get; set; }

        [JsonPropertyName("user_code")]
        public string? UserCode { get; set; }

        [JsonPropertyName("verification_uri")]
        public string? VerificationUri { get; set; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }

        [JsonPropertyName("interval")]
        public int? Interval { get; set; }
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("id_token")]
        public string? IdToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }
    }

    private class TokenErrorResponse
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; set; }
    }
}