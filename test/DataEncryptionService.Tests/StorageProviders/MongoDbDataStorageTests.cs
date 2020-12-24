using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataDataEncryptionService.Tests;
using DataEncryptionService.Configuration;
using DataEncryptionService.Integration.MongoDB.Storage;
using DataEncryptionService.Storage;
using MongoDB.Driver;
using Xunit;

namespace DataEncryptionService.Tests.StorageProviders
{
    public class MongoDbDataStorageTests
    {
        private MongoDbDataStorage _sut;
        private IMongoDatabase _database;

        private IMongoCollection<PersistedSecureData> _dataDocCollection;
        private string _connectionString;

        public MongoDbDataStorageTests()
        {
            _connectionString = "mongodb://localhost:27017/?ssl=false";

            var config = new DataEncryptionServiceConfiguration();
            config.Storage.StorageProvider = WellKnownConstants.MongoDB.StorageProviderUUID;
            config.Storage.MongoDbConnectionString = _connectionString;

            string collectionName = $"EncryptedData_(tests_on_{System.Net.Dns.GetHostName()})";
            config.Storage.EncryptedDataSetName = collectionName;

            _sut = new MongoDbDataStorage(config, TestLogger.GetLogger<MongoDbDataStorage>());

            InitializeMongoDbCollection(
                    config.Storage.MongoDbConnectionString,
                    config.Storage.DataStoreName,
                    config.Storage.EncryptedDataSetName);
        }

        private void InitializeMongoDbCollection(string connectionString, string dataStoreName, string collectionName)
        {
            MongoClient mongoDbClient = new MongoClient(connectionString);
            _database = mongoDbClient.GetDatabase(dataStoreName);
            _dataDocCollection = _database.GetCollection<PersistedSecureData>(collectionName);
        }

        [Fact]
        public void Can_Create_Configured_Provider()
        {
            // Assert
            Assert.True(_sut.IsConfigured);
        }

        [Fact]
        public async Task Verify_Document_Is_Persisted_To_Collection()
        {
            // Arrange
            PersistedSecureData doc = PersistedSecureData.CreateDefault();
            doc.EngineId = WellKnownConstants.DotNet.AesCapi.CryptoEngineUUID;
            doc.KeyName = "aes_key";
            doc.KeyVersion = 1;
            doc.Data.Add(new EncryptedDataItem()
            {
                Name = "A.1",
                Cipher = "A.2",
                Hash = "A.3"
            });

            // Act
            await _sut.SaveEncryptedDataAsync(doc);

            // Load it directly from MongoDB
            PersistedSecureData loadedDoc = await _dataDocCollection.Find(x => x.Id == doc.Id).FirstOrDefaultAsync();

            // Assert
            Assert.NotNull(loadedDoc);
            Assert.Equal(doc.EncryptedOn, loadedDoc.EncryptedOn);
            Assert.Equal(doc.CreatedOn, loadedDoc.CreatedOn);
            Assert.Equal(doc.Label, loadedDoc.Label);
            Assert.Equal(doc.EngineId, loadedDoc.EngineId);
            Assert.Equal(doc.KeyVersion, loadedDoc.KeyVersion);
            Assert.Equal(doc.KeyName, loadedDoc.KeyName);
            Assert.Equal(doc.Data.Count, loadedDoc.Data.Count);
            Assert.Equal(doc.Data[0].Name, loadedDoc.Data[0].Name);
            Assert.Equal(doc.Data[0].Cipher, loadedDoc.Data[0].Cipher);
            Assert.Equal(doc.Data[0].Hash, loadedDoc.Data[0].Hash);
        }

        [Fact]
        public async Task Verify_Document_Is_Loaded_From_Collection()
        {
            // Arrange
            PersistedSecureData doc = PersistedSecureData.CreateDefault();
            doc.EngineId = WellKnownConstants.DotNet.AesCapi.CryptoEngineUUID;
            doc.EncryptionParameters = new Dictionary<string, object>()
            {
                { "KeyName", "aes_key" }
            };
            doc.Data.Add(new EncryptedDataItem()
            {
                Name = "A.1",
                Cipher = "A.2",
                Hash = "A.3"
            });

            doc.CreatedOn = doc.EncryptedOn = new DateTime(2020, 10, 15, 12, 30, 0, DateTimeKind.Utc);

            // Save it directly to MongoDB
            await _dataDocCollection.InsertOneAsync(doc);

            // Act
            IPersistedSecureData loadedDoc = await _sut.LoadEncryptedDataAsync(doc.Label);

            // Assert
            Assert.NotNull(loadedDoc);
            Assert.Equal(doc.EncryptedOn, loadedDoc.EncryptedOn);
            Assert.Equal(doc.CreatedOn, loadedDoc.CreatedOn);
            Assert.Equal(doc.Label, loadedDoc.Label);
            Assert.Equal(doc.EngineId, loadedDoc.EngineId);
            Assert.Equal(doc.EncryptionParameters["KeyName"], loadedDoc.EncryptionParameters["KeyName"]);
            Assert.Equal(doc.Data.Count, loadedDoc.Data.Count);
            Assert.Equal(doc.Data[0].Name, loadedDoc.Data[0].Name);
            Assert.Equal(doc.Data[0].Cipher, loadedDoc.Data[0].Cipher);
            Assert.Equal(doc.Data[0].Hash, loadedDoc.Data[0].Hash);
        }

        [Fact]
        public void Can_Create_Collection_If_Not_Existant()
        {
            // Arrange
            var config = new DataEncryptionServiceConfiguration();
            config.Storage.StorageProvider = WellKnownConstants.MongoDB.StorageProviderUUID;
            config.Storage.MongoDbConnectionString = _connectionString;

            // Create a new unique name for the collection
            string collectionName = $"EncryptedData_(tests_on_{Guid.NewGuid().ToString("N")})";
            config.Storage.EncryptedDataSetName = collectionName;

            Assert.False(_database.ListCollectionNames().ToList().Exists(x => x == collectionName));

            // Act
            _sut = new MongoDbDataStorage(config, TestLogger.GetLogger<MongoDbDataStorage>());

            // Assert
            // Verify the collection has been created (during the constructor of the SUT object)
            Assert.True(_database.ListCollectionNames().ToList().Exists(x => x == collectionName));

            _database.DropCollection(collectionName);
        }

        [Fact]
        public void Validate_Provider_Metadata_Attributes()
        {
            Assert.Equal(Guid.Parse("1ca9c449-7d86-40fe-ab7b-49525113773e"), _sut.ProviderId);
            Assert.Contains("MongoDB", _sut.DisplayName);
        }
    }
}