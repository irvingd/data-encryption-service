using System.Runtime.Serialization;

namespace DataEncryptionService.Core.Telemetry.Names
{
    internal enum SpanName
    {
        [EnumMember(Value = "Data Encryption Request")]
        Encryption_Request = 1000,
        [EnumMember(Value = "Encrypting Engine Call")]
        Encryption_Crypto_Engine_Call = 1001,
        [EnumMember(Value = "Save Encrypted Data")]
        Encryption_Save_Encrypted_Data = 1002,

        [EnumMember(Value = "Data Decryption Request")]
        Decryption_Request = 2000,
        [EnumMember(Value = "Decrypting Engine Call")]
        Decryption_Crypto_Engine_Call = 2001,
        [EnumMember(Value = "Load Encrypted Data")]
        Decryption_Load_Encrypted_Data = 2002,

        [EnumMember(Value = "Data Delete Request")]
        Delete_Request = 3000,

        [EnumMember(Value = "Encryption Key Rotation Request")]
        Key_Rotation_Request = 4000,
    }
}
