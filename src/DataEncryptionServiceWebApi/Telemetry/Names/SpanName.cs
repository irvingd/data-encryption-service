using System.Runtime.Serialization;

namespace DataEncryptionService.WebApi.Telemetry.Names
{
    internal enum SpanName
    {
        [EnumMember(Value = "Token Validation Request")]
        Token_Validation_Request = 2000,
        [EnumMember(Value = "Token Vault Lookup")]
        Token_Vault_Lookup = 2001,
        [EnumMember(Value = "WebAPI Data Encryption Request")]
        Data_Encryption_Request = 2003,
        [EnumMember(Value = "WebAPI Data Decryption Request")]
        Data_Decryption_Request = 2004,
        [EnumMember(Value = "WebAPI Data Delete Request")]
        Data_Delete_Request = 2005,
    }
}