using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataEncryptionService.Configuration;
using DataEncryptionService.Core.CryptoEngines.dotNetCapi;
using DataEncryptionService.CryptoEngines;
using Xunit;

namespace DataEncryptionService.Tests.CryptoEngines
{
    public class TripleDesCapiCryptoEngineTests
    {
        private TripleDesCapiCryptoEngine _sut;
        private readonly string _clearText = "some-clear-text-data";
        private readonly string _cipherText = "X6QPCUv3ygDTDUxSvbQEH9fRr+GhxlON";

        public TripleDesCapiCryptoEngineTests()
        {
            var config = new DataEncryptionServiceConfiguration();
            config.Encryption.ActiveKeys.Add(WellKnownConstants.DotNet.TripleDesCapi.CryptoEngineUUID.ToString("N"), "3des_key");
            config.Encryption.KeyConfigurations.Add(new ServiceConfigEncryptionKeyConfiguration()
            {
                Name = "3des_key",
                Key = "el39s4RblpGdmpQoJL2OA5Vpnyv38TaP",
                IV = "tUIMCZdmraM="
            });

            _sut = new TripleDesCapiCryptoEngine(config);

            Assert.True(_sut.IsConfigured);
        }

        [Fact]
        public async Task Data_Can_Be_Encrypted()
        {
            var kvClear = new Dictionary<string, string>()
            {
                { string.Empty, _clearText }
            };

            EncryptionResult result = await _sut.EncryptAsync(kvClear);

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
            Assert.Equal(Guid.Parse("163b02dd-9e94-45c2-a52c-a0533dbbf706"), _sut.EngineId);
            Assert.Contains(".NET 3DES", _sut.DisplayName);
        }

        [Fact]
        public void Bad_Configuration_Marks_Engine_Unconfigured()
        {
            // Arrange
            string KeyName = "3des_key";
            string Key = "el39s4RblpGdmpQoJL2OA5Vpnyv38TaP";
            string IV = "tUIMCZdmraM=";
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
            var engine1 = new TripleDesCapiCryptoEngine(config1);
            var engine2 = new TripleDesCapiCryptoEngine(config2);
            var engine3 = new TripleDesCapiCryptoEngine(config3);

            // Assert
            Assert.False(engine1.IsConfigured);
            Assert.False(engine2.IsConfigured);
            Assert.False(engine3.IsConfigured);
        }
    }
}
