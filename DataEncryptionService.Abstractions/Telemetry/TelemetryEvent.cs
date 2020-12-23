using System;
using System.Collections.Generic;

namespace DataEncryptionService.Telemetry
{
    public class TelemetryEvent
    {
        public DateTime EventOn { get; set; } = DateTime.UtcNow;
        public Guid MessageId { get; set; } = Guid.NewGuid();
        public string CorrelationKey { get; set; }
        public string EventName { get; set; }
        public string EventNameKey { get; set; }
        public string Category { get; set; }
        public string Source { get; set; }
        public string SourceHostName { get; set; }
        public List<TelemetrySpan> Spans { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }
}
