using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataDataEncryptionService.Tests;
using DataEncryptionService.Configuration;
using DataEncryptionService.Core;
using DataEncryptionService.Storage;
using DataEncryptionService.Tests.CryptoEngines;
using DataEncryptionService.Tests.StorageProviders;
using Xunit;

namespace DataEncryptionService.Tests
{
    public class DataServicesManagerTests
    {
        private const string DefaultValueName = "My_Key_Name";
        private const string DefaultClearTextData = "Some-Clear-Text-Data";

        private readonly DataEncryptionManager _sut;
        //private readonly IStorageProvider _storageProvider;
        
        public DataServicesManagerTests()
        {
            // Arrange (for all tests in this class)

            // Create the Storage Factory with ONLY the in-memory storage provider
            var storageFactory = StorageProviderFactoryTests.CreateTestStorageProviderFactory();
           // _storageProvider = storageFactory.CreateProvider(); 
            _sut = CreateTestDataServicesManager(storageFactory: storageFactory);
        }

        public static DataEncryptionManager CreateTestDataServicesManager(DataEncryptionServiceConfiguration config = null, IStorageProviderFactory storageFactory = null)
        {
            config = (config is null) ? new DataEncryptionServiceConfiguration() : config;
            storageFactory = (storageFactory is null) ? StorageProviderFactoryTests.CreateTestStorageProviderFactory() : storageFactory;
            return new DataEncryptionManager(
                                        config,
                                        CryptoEngineFactoryTests.CreateTestCryptoEngineFactory(),
                                        StringHasherTests.CreateTestsStringHasher(),
                                        storageFactory,
                                        TestLogger.GetLogger<DataEncryptionManager>(),
                                        new NullTelemetrySourceClient());
        }

        [Fact]
        public async Task Can_Encrypt_Data_And_Get_Response()
        {
            // Arrange
            string keyValueName = DefaultValueName;
            var encReq = DataEncryptRequest.CreateDefault();
            encReq.Data.Add(keyValueName, DefaultClearTextData);

            // Act
            DataEncryptResponse encResp = await _sut.EncryptDataAsync(encReq);

            // Assert
            Assert.NotNull(encResp);
            Assert.Equal(ErrorCode.None, encResp.Error);

            Assert.False(string.IsNullOrEmpty(encResp.Label));

            Assert.True(encResp.Data.TryGetValue(keyValueName, out EncryptedValue encData));
            Assert.Equal(keyValueName, encData.Name);
            Assert.False(string.IsNullOrEmpty(encData.Cipher));
            Assert.False(string.IsNullOrEmpty(encData.Hash));
        }

        [Fact]
        public async Task Can_Decrypt_Data_And_Get_Response()
        {
            // Arrange
            string keyValueName = DefaultValueName;
            string clearTextData = DefaultClearTextData;
            var encReq = DataEncryptRequest.CreateDefault();
            encReq.Data.Add(keyValueName, clearTextData);

            // Act
            DataEncryptResponse encResp = await _sut.EncryptDataAsync(encReq);

            var decReq = DataDecryptRequest.CreateDefault();
            var dataItem = new LabeledEncryptedData()
            {
                Label = encResp.Label
            };
            dataItem.Items.Add(keyValueName);
            decReq.LabeledData.Add(dataItem);

            DataDecryptResponse decResp = await _sut.DecryptDataAsync(decReq);

            // Assert
            Assert.NotNull(decResp);
            Assert.Equal(ErrorCode.None, decResp.Error);

            LabeledDecryptedData decData = decResp.LabeledData.Where(x => x.Label == encResp.Label).FirstOrDefault();
            Assert.NotNull(decData);

            Assert.True(decData.Data.TryGetValue(keyValueName, out string decryptedClearText));
            Assert.Equal(clearTextData, decryptedClearText);
        }

        [Fact]
        public async Task Can_Protect_Multiple_Values_In_One_Call()
        {
            // Arrange
            string keyValueName1 = "My_Key_Name_1";
            string keyValueName2 = "My_Key_Name_2";
            string keyValueName3 = "My_Key_Name_3";

            string clearTextData1 = "Some-Value-To-Be-Encrypted-1";
            string clearTextData2 = "Some-Value-To-Be-Encrypted-2";
            string clearTextData3 = "Some-Value-To-Be-Encrypted-3";
            var encReq = DataEncryptRequest.CreateDefault();
            encReq.Data.TryAdd(keyValueName1, clearTextData1);
            encReq.Data.TryAdd(keyValueName2, clearTextData2);
            encReq.Data.TryAdd(keyValueName3, clearTextData3);

            // Act
            DataEncryptResponse encResp = await _sut.EncryptDataAsync(encReq);

            var decReq = DataDecryptRequest.CreateDefault();
            var dataItem = new LabeledEncryptedData()
            {
                Label = encResp.Label
            };
            dataItem.Items.Add(keyValueName1);
            dataItem.Items.Add(encResp.Data[keyValueName2].Hash);
            dataItem.Items.Add(keyValueName3);
            decReq.LabeledData.Add(dataItem);

            DataDecryptResponse decResp = await _sut.DecryptDataAsync(decReq);

            // Assert
            Assert.NotNull(decResp);
            Assert.Equal(ErrorCode.None, decResp.Error);
            Assert.Single(decResp.LabeledData);

            LabeledDecryptedData decData = decResp.LabeledData.Where(x => x.Label == encResp.Label).FirstOrDefault();
            Assert.NotNull(decData);
            Assert.Equal(clearTextData1, decData.Data[keyValueName1]);
            Assert.Equal(clearTextData2, decData.Data[keyValueName2]);
            Assert.Equal(clearTextData3, decData.Data[keyValueName3]);
        }

        [Fact]
        public async Task Invalid_Tag_Returns_Expected_Code()
        {
            // Arrange
            var decReq = DataDecryptRequest.CreateDefault();
            var dataItem = new LabeledEncryptedData()
            {
                Label = Guid.NewGuid().ToString("N")
            };
            dataItem.Items.Add(DefaultValueName);

            // Act
            decReq.LabeledData.Add(dataItem);

            DataDecryptResponse result = await _sut.DecryptDataAsync(decReq);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ErrorCode.None, result.Error);
            Assert.False(string.IsNullOrEmpty(result.Message));
            Assert.True(result.HasErrors);

            Assert.Single(result.LabeledData);
            Assert.Equal(ErrorCode.Storage_Labeled_Values_Not_Found, result.LabeledData[0].Error);
            Assert.False(string.IsNullOrEmpty(result.LabeledData[0].Message));
        }

        [Fact]
        public async Task RotateEncryption_With_Unsupport_EngineId_Fails()
        {
            // Arrange
            var req = RotateEncryptionRequest.CreateDefault("myscope", "mykey", CancellationToken.None);
            req.EngineId = WellKnownConstants.DotNet.AesCapi.CryptoEngineUUID;

            // Act
            RotateEncryptionResponse result = await _sut.RotateEncryptionKeyAsync(req);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ErrorCode.Crypto_Functionality_Not_Supported, result.Error);
            Assert.False(string.IsNullOrEmpty(result.Message));
        }
    }
}