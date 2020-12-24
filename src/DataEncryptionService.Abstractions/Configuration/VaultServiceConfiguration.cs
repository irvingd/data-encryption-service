using System.Collections.Generic;

namespace DataEncryptionService.Configuration
{
    public class VaultServiceConfiguration
    {
        public string ServiceUrl { get; set; }
        public string AuthMethod { get; set; }
        public int ApiTimeout { get; set; }
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
    }
}