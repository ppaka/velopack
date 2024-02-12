using Velopack.Packaging.Abstractions;
using Velopack.Packaging.Service;

namespace Velopack.Vpk.Commands;
#nullable enable

public class LoginCommandRunner(IVelopackServiceClient Client) : ICommand<LoginOptions>
{

    public async Task Run(LoginOptions options)
    {
        await Client.LoginAsync(new() {
            VelopackBaseUrl = options.VelopackBaseUrl
        });
    }
}