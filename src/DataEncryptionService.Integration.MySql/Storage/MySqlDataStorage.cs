using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataEncryptionService.Configuration;
using DataEncryptionService.Storage;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace DataEncryptionService.Integration.MySql.Storage
{
    public class MySqlDataStorage : IStorageProvider
    {
        private readonly ILogger _log;
        private readonly bool _isConfigured = false;
        private readonly System.Data.Common.DbConnection _connection;

        public MySqlDataStorage(DataEncryptionServiceConfiguration serviceConfiguration, ILogger<MySqlDataStorage> log)
        {
            _log = log;
            ServiceConfigStorage config = serviceConfiguration.Storage;
            string connectionString = config.MySqlConnectionString;
            if (!string.IsNullOrEmpty(connectionString))
            {
                _connection = new MySqlConnection(connectionString);
                _isConfigured = true;
            }
            else
            {
                if (WellKnownConstants.Postgresql.StorageProviderUUID == config.StorageProvider)
                {
                    _log.LogError("The connection string is null or empty. This provider will be disabled.");
                }
            }
        }

        public string DisplayName => WellKnownConstants.MySql.Name;
        public Guid ProviderId => WellKnownConstants.MySql.StorageProviderUUID;
        public bool IsConfigured => _isConfigured;

        public IPersistedSecureData AllocateNewData() => PersistedSecureData.CreateDefault();

        public Task SaveEncryptedDataAsync(IPersistedSecureData data)
        {
            PersistedSecureData dataDocument = data as PersistedSecureData;
            if (dataDocument is null)
            {
                throw new ArgumentException("Parameter is null or is not the expected implementation.", nameof(data));
            }

            throw new NotImplementedException();
        }

        public Task<IPersistedSecureData> LoadEncryptedDataAsync(string Label)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteEncryptedDataAsync(string label)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IPersistedSecureData>> GetEnumerableListAsync(string lastProcessedLabel, Guid? cryptoEngineId, string keyName, string keyScope, int? keyVersion, DateTime? fromEncryptedOn)
        {
            throw new NotImplementedException();
        }
    }
}