using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib.Extensions;
using DataEncryptionService.Configuration;
using DataEncryptionService.Storage;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DataEncryptionService.Integration.Postgresql.Storage
{
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// Note: This implementation needs to be refactored. Need to have a better mechanism to map PostgreSQL column names.
    ///       Consider Dapper or direct Npgsql calls.
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public class PostgresqlDataStorage : IStorageProvider
    {
        private readonly ILogger _log;
        private readonly bool _isConfigured = false;
        private readonly System.Data.Common.DbConnection _connection;

        public PostgresqlDataStorage(DataEncryptionServiceConfiguration serviceConfiguration, ILogger<PostgresqlDataStorage> log)
        {
            _log = log;
            ServiceConfigStorage config = serviceConfiguration.Storage;
            string connectionString = config.PostgresqlConnectionString;
            if (!string.IsNullOrEmpty(connectionString))
            {
                _connection = new NpgsqlConnection(connectionString);
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

        public string DisplayName => WellKnownConstants.Postgresql.Name;
        public Guid ProviderId => WellKnownConstants.Postgresql.StorageProviderUUID;
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
                var pgData = MapToPgDataRow(dataRow);
                await _connection.InsertAsync(pgData);
                dataRow.Id = pgData.row_id;
            }
            else
            {
                dataRow.EncryptedOn = timeStamp;
                await _connection.UpdateAsync(MapToPgDataRow(dataRow));
            }
        }

        private PgSecureData MapToPgDataRow(PersistedSecureData dataRow)
        {
            return new PgSecureData()
            {
                row_id = dataRow.Id,
                created_on = dataRow.CreatedOn,
                data_label = dataRow.Label,
                encrypted_on = dataRow.EncryptedOn,
                engine_id = dataRow.EngineId,
                key_scope = dataRow.KeyScope,
                key_name = dataRow.KeyName,
                key_version = dataRow.KeyVersion,
                hash_method = dataRow.HashMethod,
                engine_request_id = dataRow.EngineRequestId,
                json_data = dataRow.JsonData,
                json_enc_params = dataRow.JsonEncryptionParams,
                tags = dataRow.TagsArray
            };
        }

        const string SqlSelectClause = "SELECT" +
                                       "  row_id   Id," +
                                       "  created_on   CreatedOn," +
                                       "  data_label   \"Label\"," +
                                       "  encrypted_on   EncryptedOn," +
                                       "  engine_id   EngineId," +
                                       "  key_scope   KeyScope," +
                                       "  key_name   KeyName," +
                                       "  key_version   KeyVersion," +
                                       "  hash_method   HashMethod," +
                                       "  engine_request_id   EngineRequestId," +
                                       "  json_data   JsonData," +
                                       "  json_enc_params   JsonEncryptionParams," +
                                       "  tags   TagsArray" +
                                       " FROM encrypted_data";
        public async Task<IPersistedSecureData> LoadEncryptedDataAsync(string label)
        {
            const string SqlWhereClause = " WHERE data_label = @label";
            const string SqlStatement = SqlSelectClause + SqlWhereClause;
            return await _connection.QueryFirstOrDefaultAsync<PersistedSecureData>(SqlStatement, new { label });
        }

        public async Task<bool> DeleteEncryptedDataAsync(string label)
        {
            const string SqlStatement = "DELETE FROM encrypted_data WHERE data_label = @label";
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
                const string SqlStatement = "SELECT row_id FROM encrypted_data WHERE data_label = @label";
                id = await _connection.ExecuteScalarAsync<long>(SqlStatement, new { label = lastProcessedLabel });
                if (id > 0)
                {
                    conditions.Add($"row_id > @id");
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
                conditions.Add($"engine_id = @engineid");
            }

            if (!string.IsNullOrEmpty(keyName))
            {
                conditions.Add($"key_name = @keyname");
            }

            if (!string.IsNullOrEmpty(keyScope))
            {
                conditions.Add($"key_scope = @keyscope");
            }

            if (keyVersion.HasValue)
            {
                conditions.Add($"key_version <= @keyversion");
            }

            if (fromEncryptedOn.HasValue)
            {
                conditions.Add($"encrypted_on >= @encryptedon");
            }

            string sqlStatement = $"{SqlSelectClause} WHERE {string.Join(" AND ", conditions)}";
            return await _connection.QueryAsync<PersistedSecureData>(sqlStatement, values);
        }
    }
}