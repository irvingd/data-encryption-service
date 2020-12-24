namespace DataEncryptionService.Configuration
{
    public static class DesEnvironmentVariables
    {
        public const string VaultServerUrl = "DES_VAULT_SERVER_URL";
        public const string VaultAuthMethod = "DES_VAULT_AUTH_METHOD";
               
        public const string VaultAuthAppRoleId = "DES_VAULT_AUTH_APPROLE_ROLEID";
        public const string VaultAuthAppSecretId = "DES_VAULT_AUTH_APPROLE_SECRETID";
               
        public const string VaultAuthToken = "DES_VAULT_AUTH_TOKEN";
               
        public const string VaultAuthUserName = "DES_VAULT_AUTH_USERNAME";
        public const string VaultAuthUserPass = "DES_VAULT_AUTH_PASSWORD";
               
        public const string VaultEncryptionKey = "DES_VAULT_ENCRYPTION_KEY";
        public const string VaultEncryptionMountPoint = "DES_VAULT_ENCRYPTION_MOUNTPOINT";
               
        public const string StorageProvider = "DES_STORAGE_PROVIDER";
        public const string StorageMongoDbConnectiongString = "DES_STORAGE_MONGODB_CONNECTION_STRING";
        public const string StorageMssqlConnectiongString = "DES_STORAGE_MSSQL_CONNECTION_STRING";
        public const string StorageMySqlConnectiongString = "DES_STORAGE_MYSQL_CONNECTION_STRING";
        public const string StoragePostgresqlConnectiongString = "DES_STORAGE_POSTGRESQL_CONNECTION_STRING";

        public const string HashingMethod = "DES_HASHING_METHOD";
               
        public const string EncryptionEngineID = "DES_ENCRYPTION_ENGINEID";

        public const string PoliciesApiRequiredTokenPolicies = "DES_POLICIES_API_REQUIRED_TOKEN_POLICIES";
    }
}
