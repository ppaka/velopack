
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;
#nullable enable
namespace Velopack.Vpk.Auth;

[SupportedOSPlatform("windows")]
// Based on Azure CLI https://github.com/AzureAD/microsoft-authentication-extensions-for-python/blob/dev/msal_extensions/persistence.py
internal class WindowsCredentialStore : FileCredentialStore
{
    public override async Task<T?> RetrieveAsync<T>() where T : default
    {
        FileInfo storageFile = GetEncryptStorageFile<T>();
        if (!storageFile.Exists) {
            return default;
        }
        byte[] fileBytes = await File.ReadAllBytesAsync(storageFile.FullName);
        byte[] utf8Bytes = ProtectedData.Unprotect(fileBytes, null, DataProtectionScope.CurrentUser);
        using var stream = new MemoryStream(utf8Bytes);
        return await JsonSerializer.DeserializeAsync<T>(stream);
    }

    public override async Task StoreAsync<T>(T value) where T : default
    {
        FileInfo storageFile = GetEncryptStorageFile<T>();
        storageFile.Directory!.Create();
        using var stream = storageFile.OpenWrite();
        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(value);
        byte[] encryptedBytes = ProtectedData.Protect(jsonBytes, null, DataProtectionScope.CurrentUser);
        await stream.WriteAsync(encryptedBytes);
    }

    public override Task ClearAsync<T>()
    {
        FileInfo storageFile = GetEncryptStorageFile<T>();
        storageFile.Delete();
        return Task.CompletedTask;
    }

    private static FileInfo GetEncryptStorageFile<T>()
    {
        DirectoryInfo vpkPath = GetStorageDirectory();
        string filePath = Path.Combine(vpkPath.FullName, typeof(T).Name + ".bin");
        return new FileInfo(filePath);
    }
}
