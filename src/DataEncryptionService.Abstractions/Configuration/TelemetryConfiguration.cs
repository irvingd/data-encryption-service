using System.Collections.Generic;

namespace DataEncryptionService.Configuration
{
    public class TelemetryConfiguration
    {
        public string AppSourceName { get; set; }
        public Dictionary<string, Dictionary<string,string>> Sinks { get; set; }
    }
}