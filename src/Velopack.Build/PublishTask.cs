using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Extensions.Logging;
using Velopack.Packaging;
using Velopack.Packaging.Service;

namespace Velopack.Build;

public class PublishTask : MSBuildAsyncTask
{
    private const string ServiceUrl =
        //VelopackServiceOptions.DefaultBaseUrl;
        "http://localhost:5582/";

    private static HttpClient HttpClient { get; } = new();

    [Required]
    public string ReleaseDirectory { get; set; } = "";

    public string? Channel { get; set; }

    public string? Version { get; set; }

    protected override async Task<bool> ExecuteAsync()
    {
        //System.Diagnostics.Debugger.Launch();
        VelopackServiceClient client = new(HttpClient, Logger);
        if (!await client.LoginAsync(new() {
            AllowDeviceCodeFlow = false,
            AllowInteractiveLogin = false,
            VelopackBaseUrl = ServiceUrl
        }).ConfigureAwait(false)) {
            Logger.LogWarning("Not logged into Velopack service, skipping publish. Please run vpk login.");
            return true;
        }

        Channel ??= ReleaseEntryHelper.GetDefaultChannel(VelopackRuntimeInfo.SystemOs);
        var helper = new ReleaseEntryHelper(ReleaseDirectory, Channel, Logger);
        var latest = helper.GetLatestAssets().ToList();

        Logger.LogInformation($"Preparing to upload {latest.Count} assets to Velopack");

        foreach (var asset in latest) {

            var latestPath = Path.Combine(ReleaseDirectory, asset.FileName);

            using var fileStream = File.OpenRead(latestPath);

            await client.UploadReleaseAssetAsync(new UploadOptions(fileStream, asset.FileName, Channel) {
                VelopackBaseUrl = ServiceUrl
            }).ConfigureAwait(false);

            Logger.LogInformation($"Uploaded {asset.FileName} to Velopack");
        }
        return true;
    }
}
