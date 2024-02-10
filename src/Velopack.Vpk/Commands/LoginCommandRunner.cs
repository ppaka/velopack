using Velopack.Packaging.Abstractions;
using Velopack.Vpk.Auth;

namespace Velopack.Vpk.Commands;
#nullable enable

public class LoginCommandRunner(IAuthenticationClient authenticationClient) : ICommand<LoginOptions>
{
    private IAuthenticationClient AuthenticationClient { get; } = authenticationClient;

    public async Task Run(LoginOptions options)
    {
        await AuthenticationClient.LoginAsync(options);
    }
}