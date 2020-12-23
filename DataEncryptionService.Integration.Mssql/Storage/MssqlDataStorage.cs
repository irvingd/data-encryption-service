using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib.Extensions;
using DataEncryptionService.Configuration;
using DataEncryptionService.Storage;
using Microsoft.Extensions.Logging;

namespace DataEncryptionService.Integration.MSSQL.Storage
{
    public class MssqlDataStorage : IStorageProvider
    {
        private readonly ILogger _log;
        private readonly bool _isConfigured = false;
        private readonly System.Data.Common.DbConnection _connection;

        public MssqlDataStorage(DataEncryptionServiceConfiguration serviceConfiguration, ILogger<MssqlDataStorage> log)
        {
            _log = log;
            ServiceConfigStorage config = serviceConfiguration.Storage;
            string connectionString = config.MssqlConnectionString;
            if (!string.IsNullOrEmpty(connectionString))
            {
                _connection = new SqlConnection(connectionString);
                _isConfigured = true;
            }
            else
            {
                if (WellKnownConstants.MySql.StorageProviderUUID == config.StorageProvider)
                {
                    _log.LogError("The connection string is null or empty. This provider will be disabled.");
                }
            }
        }

        public string DisplayName => WellKnownConstants.MSSQL.Name;
        public Guid ProviderId => WellKnownConstants.MSSQL.StorageProviderUUID;
        public bool IsConfigured => _isConfigured;

        public IPersistedSecureData AllocateNewData() => PersistedSecureData.CreateDefault();

        public async Task SaveEncryptedDataAsync(IPersistedSecureData data)
        {
            PersistedSecureData dataRow = data as PersistedSecureData;
            if (dataRow is null)
            {
                string message = "Parameter is null or is not the expected implementation.";
                _log.LogError(message);
                throw new ArgumentException(message, nameof(data));
            }

            DateTime timeStamp = DateTime.UtcNow;
            if (0 == dataRow.Id)
            {
                dataRow.CreatedOn = dataRow.EncryptedOn = timeStamp;
                await _connection.InsertAsync(dataRow);
            }
            else
            {
                dataRow.EncryptedOn = timeStamp;
                await _connection.UpdateAsync(dataRow);
            }
        }

        const string SqlSelectClause = "SELECT" +
                                       "  Id," +
                                       "  CreatedOn," +
                                       "  Label," +
                                       "  EncryptedOn," +
                                       "  EngineId," +
                                       "  KeyScope," +
                                       "  KeyName," +
                                       "  KeyVersion," +
                                       "  HashMethod," +
                                       "  EngineRequestId," +
                                       "  JsonData," +
                                       "  JsonEncryptionParams," +
                                       "  AllTags" +
                                       " FROM EncryptedData";
        public async Task<IPersistedSecureData> LoadEncryptedDataAsync(string label)
        {
            const string SqlWhereClause = " WHERE Label = @label";
            const string SqlStatement = SqlSelectClause + SqlWhereClause;
            return await _connection.QueryFirstOrDefaultAsync<PersistedSecureData>(SqlStatement, new { label });
        }

        public async Task<bool> DeleteEncryptedDataAsync(string label)
        {
            const string SqlStatement = "DELETE FROM EncryptedData WHERE label = @label";
            int rowsDeleted = await _connection.ExecuteAsync(SqlStatement, new { label });
            return (rowsDeleted > 0);
        }

        public async Task<IEnumerable<IPersistedSecureData>> GetEnumerableListAsync(string lastProcessedLabel, Guid? cryptoEngineId, string keyName, string keyScope, int? keyVersion, DateTime? fromEncryptedOn)
        {
            long id = 0;
            var conditions = new List<string>();

            // Must be the first
            if (!string.IsNullOrEmpty(lastProcessedLabel))
            {
                const string SqlStatement = "SELECT Id FROM EncryptedData WHERE Label = @label";
                id = await _connection.ExecuteScalarAsync<long>(SqlStatement, new { label = lastProcessedLabel });
                if (id > 0)
                {
                    conditions.Add($"Id > @id");
                }
                else
                {
                    return new List<IPersistedSecureData>();
                }
            }

            var values = new
            {
                label = lastProcessedLabel,
                engineid = cryptoEngineId ?? null,
                keyname = keyName,
                keyscope = keyScope,
                keyversion = keyVersion,
                encryptedon = fromEncryptedOn,
                id
            };

            if (cryptoEngineId.HasValue)
            {
                conditions.Add($"EngineId = @engineid");
            }

            if (!string.IsNullOrEmpty(keyName))
            {
                conditions.Add($"KeyName = @keyname");
            }

            if (!string.IsNullOrEmpty(keyScope))
            {
                conditions.Add($"KeyScope = @keyscope");
            }

            if (keyVersion.HasValue)
            {
                conditions.Add($"KeyVersion <= @keyversion");
            }

            if (fromEncryptedOn.HasValue)
            {
                conditions.Add($"EncryptedOn >= @encryptedon");
            }

            string sqlStatement = $"{SqlSelectClause} WHERE {string.Join(" AND ", conditions)}";
            return await _connection.QueryAsync<PersistedSecureData>(sqlStatement, values);
        }
    }
}