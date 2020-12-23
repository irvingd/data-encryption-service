using DataDataEncryptionService.Tests;
using DataEncryptionService.Configuration;
using DataEncryptionService.Core.Storage;
using DataEncryptionService.Storage;
using Xunit;

namespace DataEncryptionService.Tests.StorageProviders
{
    public class StorageProviderFactoryTests
    {
        private StorageProviderFactory _sut;
        
        public StorageProviderFactoryTests()
        {
            // Arrange (for all tests in this class)
            _sut = CreateTestStorageProviderFactory();
        }

        public static StorageProviderFactory CreateTestStorageProviderFactory()
        {
            var config = new DataEncryptionServiceConfiguration();
            config.Storage.StorageProvider = InMemoryStorageProvider.UUID;

            IStorageProvider[] providers = new IStorageProvider[]
                {
                    new InMemoryStorageProvider()
                };

            return new StorageProviderFactory(config, providers, TestLogger.GetLogger<StorageProviderFactory>());
        }

        [Fact]
        public void Can_Create_Configured_StorageProvider()
        {
            // Act
            IStorageProvider storage = _sut.CreateProvider();

            // Assert
            Assert.NotNull(storage);
            Assert.Equal(InMemoryStorageProvider.UUID, storage.ProviderId);
        }

        [Fact]
        public void Can_Handle_Non_Existing_Storage_Provider()
        {
            // Arrange
            var config = new DataEncryptionServiceConfiguration();
            config.Storage.StorageProvider = InMemoryStorageProvider.UUID;

            IStorageProvider[] providers = new IStorageProvider[0];
            var local_sut = new StorageProviderFactory(new DataEncryptionServiceConfiguration(), providers, TestLogger.GetLogger<StorageProviderFactory>());

            // Act
            IStorageProvider storage = local_sut.CreateProvider();

            // Assert
            Assert.True(storage is null);
        }
    }
}
