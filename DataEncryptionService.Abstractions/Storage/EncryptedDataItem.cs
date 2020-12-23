namespace DataEncryptionService.Storage
{
    public class EncryptedDataItem
    {
        public string Name { get; set; }
        public string Cipher { get; set; }
        public string Hash { get; set; }
    }
}
