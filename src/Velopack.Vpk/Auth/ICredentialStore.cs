namespace Velopack.Vpk.Auth;
#nullable enable
public interface ICredentialStore
{
    Task StoreAsync<T>(T value);

    Task<T?> RetrieveAsync<T>();

    Task ClearAsync<T>();
}
