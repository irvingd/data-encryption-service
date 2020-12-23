namespace DataEncryptionService.CryptoEngines
{
    public interface IStringHasher
    {
        byte[] ComputeHash(string text, HashMethod method = HashMethod.SHA2_512, string hmacKey = null);
    }
}
