using System.Collections.Generic;

namespace DataEncryptionService.Configuration
{
    public class ServiceConfigApiPolicy
    {
        public List<string> RequiredTokenPolicies { get; set; } = new List<string>();
        public int TokenCacheTTL { get; set; }
    }
}