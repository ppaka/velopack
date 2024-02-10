using Velopack.Packaging.Abstractions;
using Velopack.Vpk.Auth;

namespace Velopack.Vpk.Commands;
public class LogoutCommand : BaseCommand
{
    public LogoutCommand()
        : base("logout", "Remove any stored credential to the Vellopack service.")
    {
        //Just hiding this for now as it is not ready for mass consumption.
        Hidden = true;
    }
}

internal class LogoutCommandRunner(IAuthenticationClient authenticationClient) : ICommand<LogoutOptions>
{
    private IAuthenticationClient AuthenticationClient { get; } = authenticationClient;

    public async Task Run(LogoutOptions options)
    {
        await AuthenticationClient.LogoutAsync();
    }
}