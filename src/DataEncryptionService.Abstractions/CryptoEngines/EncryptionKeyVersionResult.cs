namespace DataEncryptionService.CryptoEngines
{
    public class EncryptionKeyVersionResult
    {
        public ErrorCode Code { get; set; }
        public string Message { get; set; }
        public string Name { get; set; }
        public int CurrentVersion { get; set; }
        public int MinimumDecryptionVersion { get; set; }
        public int MinimumEncryptionVersion { get; set; }
    }
}