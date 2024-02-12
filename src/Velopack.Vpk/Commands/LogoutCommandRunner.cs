using Velopack.Packaging.Abstractions;
using Velopack.Packaging.Auth;

#nullable enable
namespace Velopack.Vpk.Commands;

internal class LogoutCommandRunner(IAuthenticationClient authenticationClient) : ICommand<LogoutOptions>
{
    private IAuthenticationClient AuthenticationClient { get; } = authenticationClient;

    public async Task Run(LogoutOptions options)
    {
        await AuthenticationClient.LogoutAsync(options);
    }
}