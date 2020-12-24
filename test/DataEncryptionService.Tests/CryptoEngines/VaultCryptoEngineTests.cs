using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataDataEncryptionService.Tests;
using DataEncryptionService.Configuration;
using DataEncryptionService.CryptoEngines;
using DataEncryptionService.Integration.Vault;
using DataEncryptionService.Integration.Vault.CryptoEngine;
using Xunit;

namespace DataEncryptionService.Tests.CryptoEngines
{
    public class VaultCryptoEngineTests
    {
        private static string DefaultValueName = "_";
        private static string DefaultClearTextData = "some-clear-text-data";

        private VaultTransitCryptoEngine _sut;
        private DataEncryptionServiceConfiguration _config;

        public VaultCryptoEngineTests()
        {
            _config = new DataEncryptionServiceConfiguration();
            _sut = CreateTestVaultCryptoEngine(_config);
            Assert.True(_sut.IsConfigured);
        }

        public static VaultTransitCryptoEngine CreateTestVaultCryptoEngine(DataEncryptionServiceConfiguration config = null)
        {
            config = (config is null) ? new DataEncryptionServiceConfiguration() : config;

            if (string.IsNullOrEmpty(config.VaultService.ServiceUrl))
            {
                config.VaultService.ServiceUrl = "http://127.0.0.1:8200";
            }
            if (string.IsNullOrEmpty(config.VaultService.AuthMethod))
            {
                config.VaultService.AuthMethod = "UserPass";
            }

            config.VaultService.Parameters.TryAdd(WellKnownConstants.Vault.Configuration.UserName, "data-encryption-service-account");
            config.VaultService.Parameters.TryAdd(WellKnownConstants.Vault.Configuration.Password, "des_acct_password");


            config.Encryption.DefaultEngine = WellKnownConstants.Vault.CryptoEngineUUID;

            var engineConfig = config.Encryption.EngineConfigurations.FirstOrDefault(x => x.EngineId == WellKnownConstants.Vault.CryptoEngineUUID);
            if (null == engineConfig)
            {
                engineConfig = new ServiceConfigEncryptionEngine() { EngineId = WellKnownConstants.Vault.CryptoEngineUUID };
                config.Encryption.EngineConfigurations.Add(engineConfig);
            }
            
            engineConfig.Parameters.TryAdd(WellKnownConstants.Vault.Configuration.DefaultKeyName, "dev_key");
            engineConfig.Parameters.TryAdd(WellKnownConstants.Vault.Configuration.DefaultMountPoint, "data_encrypt_service");


            var factory = new VaultClientFactory(TestLogger.GetLogger<VaultClientFactory>());

            return new VaultTransitCryptoEngine(config, TestLogger.GetLogger<VaultTransitCryptoEngine>(), factory);
        }

        [Fact]
        public async Task Data_Can_Be_Encrypted_With_Default_Configuration()
        {
            //////////////////////////////////////////////////////
            // Arrange
            var kvClear = new Dictionary<string, string>()
            {
                { DefaultValueName, DefaultClearTextData }
            };

            //////////////////////////////////////////////////////
            // Act
            EncryptionResult result = await _sut.EncryptAsync(kvClear);

            //////////////////////////////////////////////////////
            // Assert
            Assert.Null(result.Message);
            Assert.Equal(ErrorCode.None, result.Code);
            Assert.False(string.IsNullOrEmpty(result.RequestId));

            // Ensure the encryption operation used the configuration values
            Assert.Single(_config.Encryption.EngineConfigurations);
            ServiceConfigEncryptionEngine expectedEngineConfig = _config.Encryption.EngineConfigurations[0];
            Assert.Equal(expectedEngineConfig.Parameters[WellKnownConstants.Vault.Configuration.DefaultKeyName], result.Parameters[CommonParameterNames.KeyName].ToString());
            Assert.Equal(expectedEngineConfig.Parameters[WellKnownConstants.Vault.Configuration.DefaultMountPoint], result.Parameters[CommonParameterNames.KeyScope].ToString());

            // Validate data was actually encrypted
            Assert.Single(result.Data);
            Assert.StartsWith("vault:v", result.Data[DefaultValueName]);
        }

        [Theory]
        [MemberData(nameof(ClearData))]
        public async Task DataSets_Can_Be_Encrypted(string keyName, string[] dataNames, string[] dataValues)
        {
            Assert.Equal(dataNames.Length, dataValues.Length);

            //////////////////////////////////////////////////////
            // Arrange
            int dataCount = dataNames.Length;
            var kvClear = new Dictionary<string, string>();
            for (int i = 0; i < dataCount; i++)
            {
                kvClear.Add(dataNames[i], dataValues[i]);
            }

            var parameters = new Dictionary<string, object>()
            {
                { CommonParameterNames.KeyName, keyName }
            };

            //////////////////////////////////////////////////////
            // Act
            EncryptionResult result = await _sut.EncryptAsync(kvClear, parameters);

            //////////////////////////////////////////////////////
            // Assert
            Assert.Null(result.Message);
            Assert.Equal(ErrorCode.None, result.Code);
            Assert.True(string.IsNullOrEmpty(result.Message));

            Assert.False(string.IsNullOrEmpty(result.RequestId));

            // Ensure the encryption operation used the specified encryption key
            Assert.Equal(keyName, result.Parameters[CommonParameterNames.KeyName].ToString());

            // Validate data was actually encrypted
            Assert.Equal(dataCount, result.Data.Count);
            for (int i = 0; i < dataCount; i++)
            {
                Assert.StartsWith("vault:v", result.Data[dataNames[i]]);
            }
        }

        public static IEnumerable<object[]> ClearData()
        {
            yield return new object[] { "dev_key", new string[] { DefaultValueName }, new string[] { DefaultClearTextData } };
            yield return new object[] { "enc_key_convergent_derived", new string[] { "My_Key_Name_1", "My_Key_Name_2", "My_Key_Name_3" }, new string[] { "Some-Value-To-Be-Encrypted-1", "Some-Value-To-Be-Encrypted-1", "Some-Value-To-Be-Encrypted-3" } };
        }

        // TODO: There is a bug in Vault 1.5.4 where is does not honor the requested key version
        //       and always uses the latest key version. This test will be uncommented when a
        //       new Vault version with a fix is available.
        //
        //[Fact]
        //public async Task Data_Can_Be_Encrypted_To_Specific_KeyVersion()
        //{
        //    var kvClear = new Dictionary<string, string>()
        //    {
        //        { DefaultValueName, _clearText }
        //    };

        //    var parameters = new Dictionary<string, object>()
        //    {
        //        { VaultTransitCryptoEngine.Parameters.KeyVersion, 2 }
        //    };
        //    EncryptionResult result = await _sut.EncryptAsync(kvClear, parameters);

        //    Assert.Single(result.Data);
        //    Assert.StartsWith("vault:v2", result.Data[DefaultValueName]);
        //}

        [Fact]
        public async Task Data_Can_Be_Descrypted()
        {
            //////////////////////////////////////////////////////
            // Arrange
            var kvClear = new Dictionary<string, string>()
            {
                { DefaultValueName, DefaultClearTextData }
            };

            //////////////////////////////////////////////////////
            // Act

            // Roundtrip (Encrypt/Decrytpt) the value
            EncryptionResult encResult = await _sut.EncryptAsync(kvClear);

            Assert.Null(encResult.Message);
            Assert.Equal(ErrorCode.None, encResult.Code);
            Assert.Single(encResult.Data);
            Assert.StartsWith("vault:", encResult.Data[DefaultValueName]);

            var result = await _sut.DecryptAsync(encResult.Data, encResult.Parameters);
            IReadOnlyDictionary<string, string> kvClearActual = result.Data;
            //////////////////////////////////////////////////////
            // Assert
            Assert.Equal(ErrorCode.None, result.Code);
            Assert.Single(kvClearActual);
            Assert.Equal(DefaultClearTextData, kvClearActual[DefaultValueName]);
        }

        [Fact]
        public void Validate_Engine_Metadata_Attributes()
        {
            //////////////////////////////////////////////////////
            // Assert
            Assert.Equal(Guid.Parse("519c0525-e5d0-408f-8540-dae11c1563c3"), _sut.EngineId);
            Assert.Contains("Hashicorp Vault", _sut.DisplayName);
        }

        [Fact]
        public async Task Empty_Value_Name_Fails_Encryption_With_Specific_Code()
        {
            //////////////////////////////////////////////////////
            // Arrange
            var kvClear = new Dictionary<string, string>()
            {
                { string.Empty, DefaultClearTextData },
            };

            //////////////////////////////////////////////////////
            // Act
            EncryptionResult result = await _sut.EncryptAsync(kvClear);

            //////////////////////////////////////////////////////
            // Assert

            // Check expected error code
            Assert.Equal(ErrorCode.Crypto_Encryption_Context_Not_Set, result.Code);
            Assert.False(string.IsNullOrEmpty(result.Message));

            // Nothing returned
            Assert.Null(result.Data);
            Assert.Null(result.Parameters);
        }

        [Fact]
        public async Task Encrypting_With_Non_Existing_Key_Returns_Access_Denied()
        {
            //////////////////////////////////////////////////////
            // Arrange
            var kvClear = new Dictionary<string, string>()
            {
                { DefaultClearTextData, DefaultClearTextData },
            };

            var parameters = new Dictionary<string, object>()
            {
                { CommonParameterNames.KeyName, Guid.NewGuid().ToString("N") }
            };

            //////////////////////////////////////////////////////
            // Act
            EncryptionResult result = await _sut.EncryptAsync(kvClear, parameters);

            //////////////////////////////////////////////////////
            // Assert

            // Check expected error code
            Assert.Equal(ErrorCode.Crypto_Access_Denied_To_Key_or_Service, result.Code);
            Assert.False(string.IsNullOrEmpty(result.Message));

            // Nothing returned
            Assert.Null(result.Data);
            Assert.Null(result.Parameters);
        }

        [Fact]
        public async Task Unreachable_Server_Returns_Network_Error()
        {
            //////////////////////////////////////////////////////
            // Arrange
            var kvClear = new Dictionary<string, string>()
            {
                { DefaultClearTextData, DefaultClearTextData },
            };

            var config = new DataEncryptionServiceConfiguration();
            config.Encryption.DefaultEngine = WellKnownConstants.Vault.CryptoEngineUUID;
            var engineConfig = new ServiceConfigEncryptionEngine() { EngineId = WellKnownConstants.Vault.CryptoEngineUUID, };
            config.Encryption.EngineConfigurations.Add(engineConfig);
            
            config.VaultService.ServiceUrl = "http://127.0.0.1:12345";
            //config.VaultService.ApiTimeout = 5; // in seconds, for a quick test run
            var localSut = CreateTestVaultCryptoEngine(config);

            //////////////////////////////////////////////////////
            // Act
            EncryptionResult result = await localSut.EncryptAsync(kvClear);

            //////////////////////////////////////////////////////
            // Assert

            // Check expected error code
            Assert.Equal(ErrorCode.Network_Connection_Error, result.Code);
            Assert.False(string.IsNullOrEmpty(result.Message));

            // Nothing returned
            Assert.Null(result.Data);
            Assert.Null(result.Parameters);
        }
    }
}
