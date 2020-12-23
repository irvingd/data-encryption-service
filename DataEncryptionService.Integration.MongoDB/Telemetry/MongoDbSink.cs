using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DataEncryptionService.Configuration;
using DataEncryptionService.Telemetry;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace DataEncryptionService.Integration.MongoDB.Telemetry
{
    public class MongoDbSink : ITelemetrySink
    {
        public const string SinkName = "MongoDB";
        public static class ConfigParameterNames
        {
            public const string ConnectionString = "ConnectionString";
            public const string Database = "Database";
            public const string Collection = "Collection";
        }

        private class PersistedEventDataInfo
        {
            public ObjectId Id { get; set; }
            public DateTime CreatedOn { get; set; }
            public string EventName { get; set; }
            public string Category { get; set; }
            public DateTime EventOn { get; set; }
            public long? DurationMs { get; set; }
            public long? DurationTicks { get; set; }
            public string CorrelationKey { get; set; }
            public string SerializedEvent { get; set; }
        }

        private readonly ILogger _log;
        private readonly MongoClient _mongoDbClient;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<PersistedEventDataInfo> _dataDocCollection;
        private readonly bool _isConfigured = false;
        private readonly JsonSerializerOptions _defaultJsonSerializationOptions = new JsonSerializerOptions() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

        public MongoDbSink(TelemetryConfiguration config, ILogger<MongoDbSink> log)
        {
            _log = log;

            // If the Sinks contains an entry fo this sink, it should be configured
            // Otherwise, it means this sink should NOT be used (the _isConfigured flag will remain FALSE)
            if (config.Sinks?.ContainsKey(SinkName) == true)
            {
                string connectionString = GetParameter(config, ConfigParameterNames.ConnectionString);
                if (!string.IsNullOrEmpty(connectionString))
                {
                    string databaseName = GetParameter(config, ConfigParameterNames.Database);
                    if (!string.IsNullOrEmpty(databaseName))
                    {
                        string collectionName = GetParameter(config, ConfigParameterNames.Collection);
                        if (!string.IsNullOrEmpty(collectionName))
                        {
                            ConventionRegistry.Register("IgnoreIfDefault", new ConventionPack { new IgnoreIfDefaultConvention(true) }, t => true);

                            _mongoDbClient = new MongoClient(connectionString);
                            _database = _mongoDbClient.GetDatabase(databaseName);
                            _dataDocCollection = _database.GetCollection<PersistedEventDataInfo>(collectionName);
                            _isConfigured = true;
                        }
                        else
                        {
                            log.LogError("The collection name name is null or empty.");
                        }
                    }
                    else
                    {
                        log.LogError("The database name is null or empty.");
                    }
                }
                else
                {
                    _log.LogError("The connection string is null or empty.");
                }
            }
        }

        private string GetParameter(TelemetryConfiguration config, string parameterName)
        {
            config.Sinks.TryGetValue(SinkName, out Dictionary<string, string> parameters);

            string value = null;
            parameters?.TryGetValue(parameterName, out value);
            return value;
        }

        public string Name => SinkName;

        public async Task CommitEvent(TelemetryEvent eventData)
        {
            if (_isConfigured)
            {
                (long? durationMs, long? durationTicks) = GetDuration(eventData.Spans);
                var dataDocument = new PersistedEventDataInfo()
                {
                    CreatedOn = DateTime.UtcNow,
                    EventName = eventData.EventName,
                    Category = eventData.Category,
                    EventOn = eventData.EventOn,
                    DurationMs = durationMs,
                    DurationTicks = durationTicks,
                    CorrelationKey = eventData.CorrelationKey,

                    // This is serialized to a string because it preserves the full precision of the
                    // DateTime value. The MongoDB JSON serializer truncates the DateTime values.
                    SerializedEvent = JsonSerializer.Serialize(eventData, _defaultJsonSerializationOptions)
                };

                try
                {
                    await _dataDocCollection.InsertOneAsync(dataDocument);
                }
                catch (Exception e)
                {
                    _log.LogError(e, "Cannot insert event data");
                }
            }
        }

        private (long?, long?) GetDuration(List<TelemetrySpan> spans)
        {
            TelemetrySpan span = spans?.Find(p => p.NestLevel == 1);
            if (null == span)
            {
                return (null, null);
            }

            return (span.ElapsedMs, span.ElapsedTicks);
        }
    }
}