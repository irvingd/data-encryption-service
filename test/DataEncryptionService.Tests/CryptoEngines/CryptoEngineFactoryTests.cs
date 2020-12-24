using DataEncryptionService.Configuration;
using DataEncryptionService.Core.CryptoEngines;
using DataEncryptionService.CryptoEngines;
using Xunit;

namespace DataEncryptionService.Tests.CryptoEngines
{
    public class CryptoEngineFactoryTests
    {
        private CryptoEngineFactory _sut;
        
        public CryptoEngineFactoryTests()
        {
            // Arrange (for all tests in this class)
            _sut = CreateTestCryptoEngineFactory();
        }

        public static CryptoEngineFactory CreateTestCryptoEngineFactory()
        {
            var config = new DataEncryptionServiceConfiguration();
            config.Storage.StorageProvider = InMemoryStorageProvider.UUID;

            ICryptographicEngine[] engines = new ICryptographicEngine[]
                {
                    AesCapiCryptoEngineTests.CreateTestAesCryptoEngine()
                };

            return new CryptoEngineFactory(config, engines);
        }

        [Fact]
        public void Can_Create_Configured_Cryptographic_Engine()
        {
            // Act
            ICryptographicEngine engine = _sut.GetDefaultEngine();

            // Assert
            Assert.NotNull(engine);
            Assert.Equal(WellKnownConstants.DotNet.AesCapi.CryptoEngineUUID, engine.EngineId);
        }

        //[Fact]
        //public void Can_Handle_Non_Existing_Storage_Provider()
        //{
        //    // Arrange
        //    var config = new DataEncryptionServiceConfiguration();
        //    config.Storage.StorageProvider = InMemoryStorageProvider.EngineUuid;

        //    IStorageProvider[] providers = new IStorageProvider[0];
        //    var local_sut = new StorageProviderFactory(new DataEncryptionServiceConfiguration(), providers);

        //    // Act
        //    IStorageProvider storage = local_sut.CreateProvider();

        //    // Assert
        //    Assert.True(storage is null);
        //}
    }
}
