namespace DataEncryptionService.Storage
{
    public interface IStorageProviderFactory
    {
        IStorageProvider CreateProvider();
    }
}