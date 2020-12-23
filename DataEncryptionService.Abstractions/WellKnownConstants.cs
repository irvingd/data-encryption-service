using System;

namespace DataEncryptionService
{
    public static class WellKnownConstants
    {
        public static class MongoDB
        {
            public const string Name = "MongoDB Storage Provider";
            public readonly static Guid StorageProviderUUID = Guid.Parse("1ca9c449-7d86-40fe-ab7b-49525113773e");
        }

        public static class MySql
        {
            public const string Name = "MySql Storage Provider";
            public readonly static Guid StorageProviderUUID = Guid.Parse("3240baaf-19f3-4d01-b580-604694faf904");
        }

        public static class MSSQL
        {
            public const string Name = "Microsoft SQL Server Storage Provider";
            public readonly static Guid StorageProviderUUID = Guid.Parse("044A4917-A96F-4B00-B60E-BC100CCBDB92");
        }

        public static class Postgresql
        {
            public const string Name = "PostgreSQL Storage Provider";
            public readonly static Guid StorageProviderUUID = Guid.Parse("AB9264C9-2D2B-4010-ABBE-36E832633A97");
        }

        public static class Vault
        {
            public readonly static Guid CryptoEngineUUID = Guid.Parse("519c0525-e5d0-408f-8540-dae11c1563c3");

            public static class Parameter
            {
                public const string KeyType = "KeyType";
            }

            public static class Configuration
            {
                public const string DefaultKeyName = "DefaultKeyName";
                public const string DefaultMountPoint = "DefaultMountPoint";

                public const string Token = "Token";

                public const string UserName = "UserName";
                public const string Password = "Password";

                public const string RoleId = "RoleId";
                public const string SecretId = "SecretId";
            }
        }

        public static class DotNet
        {
            public static class AesCapi
            {
                public readonly static Guid CryptoEngineUUID = Guid.Parse("c87aa1e4-5f97-4a33-8d5a-798835dcb7b1");
                public const string Name = ".NET AES Crypto Engine (CAPI)";
            }

            public static class TripleDesCapi
            {
                public readonly static Guid CryptoEngineUUID = Guid.Parse("163b02dd-9e94-45c2-a52c-a0533dbbf706");
                public const string Name = ".NET 3DES Crypto Engine (CAPI)";
            }
        }
    }
}
