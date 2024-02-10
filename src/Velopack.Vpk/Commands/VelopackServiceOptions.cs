namespace Velopack.Vpk.Commands;

#nullable enable
public class VelopackServiceOptions
{
    public const string DefaultBaseUrl = "https://api.velopack.io/";

    public string VelopackBaseUrl { get; set; } = DefaultBaseUrl;
}

public sealed class LoginOptions: VelopackServiceOptions;

public sealed class LogoutOptions: VelopackServiceOptions;
