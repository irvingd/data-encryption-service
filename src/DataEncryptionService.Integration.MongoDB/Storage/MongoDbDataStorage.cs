using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataEncryptionService.Configuration;
using DataEncryptionService.Storage;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace DataEncryptionService.Integration.MongoDB.Storage
{
    public class MongoDbDataStorage : IStorageProvider
    {
        private readonly ILogger _log;
        private readonly MongoClient _mongoDbClient;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<PersistedSecureData> _dataDocCollection;
        private readonly bool _isConfigured = false;

        public MongoDbDataStorage(DataEncryptionServiceConfiguration serviceConfiguration, ILogger<MongoDbDataStorage> log)
        {
            _log = log;
            ServiceConfigStorage config = serviceConfiguration.Storage;
            string connectionString = config.MongoDbConnectionString;
            if (!string.IsNullOrEmpty(connectionString))
            {
                ConventionRegistry.Register("IgnoreIfDefault", new ConventionPack { new IgnoreIfDefaultConvention(true) }, t => true);

                _mongoDbClient = new MongoClient(connectionString);
                _database = _mongoDbClient.GetDatabase(config.DataStoreName);

                if (config.CreateDataSetAsNeeded)
                {
                    CreateCollectionAsNeeded(config.EncryptedDataSetName);
                }

                _dataDocCollection = _database.GetCollection<PersistedSecureData>(config.EncryptedDataSetName);
                _isConfigured = true;
            }
            else
            {
                if (WellKnownConstants.MongoDB.StorageProviderUUID == config.StorageProvider)
                {
                    _log.LogError("The connection string is null or empty. This provider will be disabled.");
                }
            }
        }

        private void CreateCollectionAsNeeded(string collectionName)
        {
            if (!_database.ListCollectionNames().ToList().Exists(x => x == collectionName))
            {
                _database.CreateCollection(collectionName);
            }
        }

        public string DisplayName => WellKnownConstants.MongoDB.Name;
        public Guid ProviderId => WellKnownConstants.MongoDB.StorageProviderUUID;
        public bool IsConfigured => _isConfigured;

        public IPersistedSecureData AllocateNewData() => PersistedSecureData.CreateDefault();

        public async Task SaveEncryptedDataAsync(IPersistedSecureData data)
        {
            var dataDocument = data as PersistedSecureData;
            if (dataDocument is null)
            {
                throw new ArgumentException("Parameter is null or is not the expected implementation.", nameof(data));
            }

            DateTime timeStamp = DateTime.UtcNow;
            if (ObjectId.Empty == dataDocument.Id)
            {
                dataDocument.CreatedOn = dataDocument.EncryptedOn = new DateTime(timeStamp.Year, timeStamp.Month, timeStamp.Day, timeStamp.Hour, timeStamp.Minute, timeStamp.Second, DateTimeKind.Utc);
                await _dataDocCollection.InsertOneAsync(dataDocument);
            }
            else
            {
                dataDocument.EncryptedOn = new DateTime(timeStamp.Year, timeStamp.Month, timeStamp.Day, timeStamp.Hour, timeStamp.Minute, timeStamp.Second, DateTimeKind.Utc);
                var filter = Builders<PersistedSecureData>.Filter.Eq(x => x.Id, dataDocument.Id);
                var result = await _dataDocCollection.ReplaceOneAsync(filter, dataDocument, options: new ReplaceOptions { IsUpsert = true });
                // TODO: if the update does not modifiy any document, raise an exception
            }
        }

        public async Task<IPersistedSecureData> LoadEncryptedDataAsync(string Label)
        {
            PersistedSecureData item = await _dataDocCollection.Find(x => x.Label == Label).FirstOrDefaultAsync();
            return item;
        }

        public async Task<bool> DeleteEncryptedDataAsync(string label)
        {
            DeleteResult result = await _dataDocCollection.DeleteOneAsync(x => x.Label == label);
            return (result.DeletedCount > 0);
        }

        public async Task<IEnumerable<IPersistedSecureData>> GetEnumerableListAsync(string lastProcessedLabel, Guid? cryptoEngineId, string keyName, string keyScope, int? keyVersion, DateTime? fromEncryptedOn)
        {
            var filter = Builders<PersistedSecureData>.Filter.Empty;

            // Must be the first
            if (!string.IsNullOrEmpty(lastProcessedLabel))
            {
                var startingDoc = await _dataDocCollection.Find(x => x.Label == lastProcessedLabel).FirstOrDefaultAsync();
                if (null != startingDoc)
                {
                    filter = Builders<PersistedSecureData>.Filter.Gte(x => x.Id, startingDoc.Id);
                }
                else
                {
                    return new List<IPersistedSecureData>();
                }
            }

            if (cryptoEngineId.HasValue)
            {
                filter &= Builders<PersistedSecureData>.Filter.Eq(x => x.EngineId, cryptoEngineId.Value);
            }

            if (!string.IsNullOrEmpty(keyName))
            {
                filter &= Builders<PersistedSecureData>.Filter.Eq(x => x.KeyName, keyName);
            }

            if (!string.IsNullOrEmpty(keyScope))
            {
                filter &= Builders<PersistedSecureData>.Filter.Eq(x => x.KeyScope, keyScope);
            }

            if (keyVersion.HasValue)
            {
                filter &= Builders<PersistedSecureData>.Filter.Lte(x => x.KeyVersion, keyVersion.Value);
            }

            if (fromEncryptedOn.HasValue)
            {
                filter = Builders<PersistedSecureData>.Filter.Gte(x => x.EncryptedOn, fromEncryptedOn.Value);
            }

            var cursor = await _dataDocCollection.FindAsync(filter);
            return cursor.ToEnumerable();
        }
    }
}