using Velopack.Vpk.Commands;

namespace Velopack.Vpk.Auth;
#nullable enable
public interface IAuthenticationClient
{
    Task<bool> LoginAsync(VelopackServiceOptions options);

    Task LogoutAsync(VelopackServiceOptions options);
}
