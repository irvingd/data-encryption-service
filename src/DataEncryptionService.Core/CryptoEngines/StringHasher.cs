using System;
using System.Security.Cryptography;
using System.Text;
using DataEncryptionService.CryptoEngines;

namespace DataEncryptionService.Core.CryptoEngines
{
    public class StringHasher : IStringHasher
    {
        public byte[] ComputeHash(string text, HashMethod method = HashMethod.SHA2_512, string hmacKey = null)
        {
            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            byte[] keyBytes = string.IsNullOrEmpty(hmacKey) ? null : Encoding.UTF8.GetBytes(hmacKey);

            HashAlgorithm hasher = (method switch
            {
                HashMethod.HMAC256 => new HMACSHA256(keyBytes),
                HashMethod.HMAC384 => new HMACSHA384(keyBytes),
                HashMethod.HMAC512 => new HMACSHA512(keyBytes),
                HashMethod.SHA2_256 => SHA256.Create(),
                HashMethod.SHA2_384 => SHA384.Create(),
                HashMethod.SHA2_512 => SHA512.Create(),
                _ => throw new NotSupportedException($"Hashing method {method} not supported."),
            });

            return hasher.ComputeHash(textBytes);
        }
    }
}
