namespace DataEncryptionService.Configuration
{
    public class ServiceConfigHashing
    {
        public HashMethod DefaultHash { get; set; } = HashMethod.SHA2_512;
        public string HashKey { get; set; }
    }
}