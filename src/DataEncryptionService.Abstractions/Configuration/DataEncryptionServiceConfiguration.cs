namespace DataEncryptionService.Configuration
{
    public class DataEncryptionServiceConfiguration
    {
        public ServiceConfigHashing Hashing { get; set; } = new ServiceConfigHashing();
        public ServiceConfigEncryption Encryption { get; set; } = new ServiceConfigEncryption();
        public ServiceConfigStorage Storage { get; set; } = new ServiceConfigStorage();
        public VaultServiceConfiguration VaultService { get; set; } = new VaultServiceConfiguration();
        public TelemetryConfiguration Telemetry { get; set; } = new TelemetryConfiguration();
        public ServiceConfigPolicies Policies { get; set; } = new ServiceConfigPolicies();
    }
}