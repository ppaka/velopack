using Velopack.Packaging.Abstractions;
using Velopack.Packaging.Service;

#nullable enable
namespace Velopack.Vpk.Commands;

internal class LogoutCommandRunner(IVelopackServiceClient Client) : ICommand<LogoutOptions>
{
    public async Task Run(LogoutOptions options)
    {
        await Client.LogoutAsync(options);
    }
}