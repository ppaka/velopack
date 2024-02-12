namespace Velopack.Packaging.Auth;

#nullable enable
public interface IAuthenticationClient
{
    Task<bool> LoginAsync(VelopackServiceOptions options);

    Task LogoutAsync(VelopackServiceOptions options);
}
