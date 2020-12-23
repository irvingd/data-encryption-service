using System.Collections.Generic;
using System.Threading.Tasks;
using DataEncryptionService.Telemetry;
using DataEncryptionService.WebApi.Telemetry.Names;

namespace DataEncryptionService.WebApi.Telemetry
{
    internal static class TelemetrySourceClientExtension
    {
        public static Task RaiseEventAsync(this ITelemetrySourceClient instance, EventName eventName, IEnumerable<TelemetrySpan> associatedSpans = null, string correlationKey = null, Dictionary<string, object> metadata = null)
        {
            return instance.RaiseEventAsync(eventName.ToString(), ((int)eventName).ToString(), associatedSpans, correlationKey, metadata);
        }
    }
}