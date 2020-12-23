using System;

namespace DataEncryptionService.Configuration
{
    public class ServiceConfigStorage
    {
        public Guid StorageProvider { get; set; } = WellKnownConstants.MongoDB.StorageProviderUUID;
        public string MongoDbConnectionString { get; set; }
        public string PostgresqlConnectionString { get; set; }
        public string MySqlConnectionString { get; set; }
        public string MssqlConnectionString { get; set; }

        public string DataStoreName { get; set; } = "SecureDataStore";
        public string EncryptedDataSetName { get; set; } = "EncryptedData";
        public bool CreateDataSetAsNeeded { get; set; } = true;
    }
}