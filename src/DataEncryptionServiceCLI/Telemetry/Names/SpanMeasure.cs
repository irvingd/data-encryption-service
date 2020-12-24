using System.Collections.Generic;
using DataEncryptionService.Telemetry;

namespace DataEncryptionService.CLI.Telemetry.Names
{
    internal static class SpanMeasure
    {
        public static TelemetrySpanMeasure Start(SpanName name, ICollection<TelemetrySpan> collectedSpans, TelemetrySpanMeasure parentSpan = null, IEnumerable<string> tags = null)
        {
            return TelemetrySpanMeasure.Start(name.ToString(), ((int)name).ToString(), collectedSpans, parentSpan, tags);
        }
    }
}