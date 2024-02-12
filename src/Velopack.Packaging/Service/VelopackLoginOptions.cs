#if NET6_0_OR_GREATER
#endif

#nullable enable
namespace Velopack.Packaging.Service;

public class VelopackLoginOptions : VelopackServiceOptions
{
    public bool AllowCacheCredentials { get; set; } = true;
    public bool AllowInteractiveLogin { get; set; } = true;
    public bool AllowDeviceCodeFlow { get; set; } = true;
}
