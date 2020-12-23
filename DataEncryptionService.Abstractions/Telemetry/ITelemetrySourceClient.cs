using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataEncryptionService.Telemetry
{
    public interface ITelemetrySourceClient
    {
        Task RaiseEventAsync(string eventName, string eventNameKey, IEnumerable<TelemetrySpan> associatedSpans = null, string correlationKey = null, Dictionary<string, object> metadata = null);
        Task RaiseErrorAsync(Exception exception, string errorMessage = null, string correlationKey = null, Dictionary<string, object> metadata = null);
        Task RaiseWarningAsync(string message, IEnumerable<TelemetryWarningTuple> warnings, string correlationKey = null, Dictionary<string, object> metadata = null);
        Task RaiseErrorAsync(int errorCode, string errorMessage = null, string correlationKey = null, Dictionary<string, object> metadata = null);
    }
}
