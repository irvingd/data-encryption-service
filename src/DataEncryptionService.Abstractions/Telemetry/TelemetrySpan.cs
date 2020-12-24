using System.Collections.Generic;

namespace DataEncryptionService.Telemetry
{
    public class TelemetrySpan
    {
        public string Name { get; set; }
        public string NameKey { get; set; }
        public long ElapsedTicks { get; set; }
        public long ElapsedMs { get; set; }
        public List<string> Tags { get; set; }
        public Dictionary<string, object> MetaData { get; set; }
        public int NestLevel { get; set; }
    }
}