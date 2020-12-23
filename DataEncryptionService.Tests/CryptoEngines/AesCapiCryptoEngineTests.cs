using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataEncryptionService.Configuration;
using DataEncryptionService.Core.CryptoEngines.dotNetCapi;
using DataEncryptionService.CryptoEngines;
using Xunit;

namespace DataEncryptionService.Tests.CryptoEngines
{
    public class AesCapiCryptoEngineTests
    {
        private AesCapiCryptoEngine _sut;
        private readonly string _clearText = "some-clear-text-data";
        private readonly string _cipherText = "Nz8ppNt+YRKgn9LFWVAt/gWLQtJITxQutq9Th7udF8o=";

        public AesCapiCryptoEngineTests()
        {
            _sut = CreateTestAesCryptoEngine();
            Assert.True(_sut.IsConfigured);
        }

        public static AesCapiCryptoEngine CreateTestAesCryptoEngine()
        {
            var config = new DataEncryptionServiceConfiguration();
            config.Encryption.ActiveKeys.Add(WellKnownConstants.DotNet.AesCapi.CryptoEngineUUID.ToString("N"), "aes_key");
            config.Encryption.KeyConfigurations.Add(new ServiceConfigEncryptionKeyConfiguration()
            {
                Name = "aes_key",
                Key = "5FWpu4ZJqe5VR5LiBkwcqHGvwgOF1mdkZOMohwDmrmI=",
                IV = "QYUo16NhdqdSCwW1ccfh2w=="
            });

            return new AesCapiCryptoEngine(config);
        }

        [Fact]
        public async Task Data_Can_Be_Encrypted()
        {
            var kvClear = new Dictionary<string, string>()
            {
                { string.Empty, _clearText }
            };

            EncryptionResult result =  await _sut.EncryptAsync(kvClear);

            Assert.Single(result.Data);
            Assert.Equal(_cipherText, result.Data[string.Empty]);
        }

        [Fact]
        public async Task Data_Can_Be_Descrypted()
        {
            var kvCipher = new Dictionary<string, string>()
            {
                { string.Empty, _cipherText }
            };

            var result = await _sut.DecryptAsync(kvCipher);

            IReadOnlyDictionary<string, string> kvClear = result.Data;
            Assert.Equal(1, kvClear.Count);
            Assert.Equal(_clearText, kvClear[string.Empty]);
        }

        [Fact]
        public void Validate_Engine_Metadata_Attributes()
        {
            Assert.Equal(Guid.Parse("c87aa1e4-5f97-4a33-8d5a-798835dcb7b1"), _sut.EngineId);
            Assert.Contains(".NET AES", _sut.DisplayName);
        }

        [Fact]
        public void Bad_Configuration_Marks_Engine_Unconfigured()
        {
            // Arrange
            string KeyName = "aes_key";
            string Key = "5FWpu4ZJqe5VR5LiBkwcqHGvwgOF1mdkZOMohwDmrmI=";
            string IV = "QYUo16NhdqdSCwW1ccfh2w==";
            // Config 1 - Missing encryption key
            var config1 = new DataEncryptionServiceConfiguration();
            // Config 2 - Encryption key is missing parts
            var config2 = new DataEncryptionServiceConfiguration();
            config2.Encryption.ActiveKeys.Add(_sut.EngineId.ToString("N"), KeyName);
            config2.Encryption.KeyConfigurations.Add(new ServiceConfigEncryptionKeyConfiguration()
            {
                Name = KeyName,
                IV = IV
            });
            // Config 3 - Encryption key is missing parts
            var config3 = new DataEncryptionServiceConfiguration();
            config3.Encryption.ActiveKeys.Add(_sut.EngineId.ToString("N"), KeyName);
            config3.Encryption.KeyConfigurations.Add(new ServiceConfigEncryptionKeyConfiguration()
            {
                Name = KeyName,
                Key = Key
            });

            // Act
            var engine1 = new AesCapiCryptoEngine(config1);
            var engine2 = new AesCapiCryptoEngine(config2);
            var engine3 = new AesCapiCryptoEngine(config3);

            // Assert
            Assert.False(engine1.IsConfigured);
            Assert.False(engine2.IsConfigured);
            Assert.False(engine3.IsConfigured);
        }
    }
}
