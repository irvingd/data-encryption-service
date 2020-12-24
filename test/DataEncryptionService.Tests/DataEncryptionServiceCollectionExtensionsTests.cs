using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DataEncryptionService.Configuration;
using DataEncryptionService.CryptoEngines;
using DataEncryptionService.Integration.MongoDB;
using DataEncryptionService.Integration.MySql;
using DataEncryptionService.Integration.Vault;
using DataEncryptionService.Storage;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;

namespace DataEncryptionService.Tests
{
    public class DataEncryptionServiceCollectionExtensionsTests
    {
        public DataEncryptionServiceCollectionExtensionsTests()
        {
        }

        [Fact]
        public void Can_Add_Interfaces_To_ServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            IServiceProvider sut = services
                                        .AddLogging()
                                        .AddDataEncryptionServices()
                                        .AddDataEncryptionServiceMongoDbIntegration()
                                        .AddDataEncryptionServiceVaultIntegration()
                                        .AddDataEncryptionServiceMySqlIntegration()
                                        .BuildServiceProvider();

            // Assert
            IStorageProviderFactory storageProvidersFactory = sut.GetRequiredService<IStorageProviderFactory>();
            Assert.NotNull(storageProvidersFactory);

            IEnumerable<IStorageProvider> storageProviders = sut.GetServices<IStorageProvider>();
            Assert.NotNull(storageProviders);
            Assert.True(storageProviders.Any());

            ICryptoEngineFactory cryptoEngineFactory = sut.GetRequiredService<ICryptoEngineFactory>();
            Assert.NotNull(cryptoEngineFactory);

            IEnumerable<ICryptographicEngine> cryptoEngines = sut.GetServices<ICryptographicEngine>();
            Assert.NotNull(cryptoEngines);
            Assert.True(cryptoEngines.Count() >= 3);
        }

        [Fact]
        public void Can_Read_Config_Stream_And_Return_Configured_Interface()
        {
            // Arrange
            var services = new ServiceCollection();
            var config = new DataEncryptionServiceConfiguration();
            config.Encryption.ActiveKeys.Add(WellKnownConstants.DotNet.AesCapi.CryptoEngineUUID.ToString("N"), "aes_key");
            config.Encryption.KeyConfigurations.Add(new ServiceConfigEncryptionKeyConfiguration()
            {
                Name = "aes_key",
                Key = "5FWpu4ZJqe5VR5LiBkwcqHGvwgOF1mdkZOMohwDmrmI=",
                IV = "QYUo16NhdqdSCwW1ccfh2w=="
            });
            string serializedConfig = JsonConvert.SerializeObject(config);
            var configStream = new MemoryStream(Encoding.ASCII.GetBytes(serializedConfig));

            // Act
            IServiceProvider sut = services
                                        .AddLogging()
                                        .AddDataEncryptionServices(configStream: configStream)
                                        .BuildServiceProvider();

            // Assert
            ICryptoEngineFactory cryptoEngineFactory = sut.GetRequiredService<ICryptoEngineFactory>();
            Assert.NotNull(cryptoEngineFactory);

            ICryptographicEngine cryptoEngine = cryptoEngineFactory.GetDefaultEngine();
            Assert.NotNull(cryptoEngine);
            Assert.True(cryptoEngine.IsConfigured);
        }
    }
}
