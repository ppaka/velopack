using System.Text.Json;
#nullable enable
namespace Velopack.Vpk.Auth;

internal class FileCredentialStore : ICredentialStore
{
    public virtual async Task<T?> RetrieveAsync<T>()
    {
        FileInfo storageFile = GetStorageFile<T>();
        if (!storageFile.Exists) {
            return default;
        }
        using var stream = storageFile.OpenRead();
        return await JsonSerializer.DeserializeAsync<T>(stream);
    }

    public virtual async Task StoreAsync<T>(T value)
    {
        FileInfo storageFile = GetStorageFile<T>();
        storageFile.Directory!.Create();
        using var stream = storageFile.OpenWrite();
        await JsonSerializer.SerializeAsync(stream, value);
    }

    protected static FileInfo GetStorageFile<T>()
    {
        DirectoryInfo vpkPath = GetStorageDirectory();
        string filePath = Path.Combine(vpkPath.FullName, typeof(T).Name + ".json");
        return new FileInfo(filePath);
    }

    protected static DirectoryInfo GetStorageDirectory()
    {
        string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string vpkPath = Path.Combine(userPath, ".vpk");
        return new DirectoryInfo(vpkPath);
    }

    public virtual Task ClearAsync<T>()
    {
        FileInfo storageFile = GetStorageFile<T>();
        storageFile.Delete();
        return Task.CompletedTask;
    }
}