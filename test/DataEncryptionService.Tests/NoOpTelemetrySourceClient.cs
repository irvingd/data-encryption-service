using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataEncryptionService.Core.Telemetry;
using DataEncryptionService.Telemetry;

namespace DataEncryptionService.Tests
{
    public class NullTelemetrySourceClient : ITelemetrySourceClient
    {
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public Task RaiseErrorAsync(Exception exception, string errorMessage = null, string correlationKey = null, Dictionary<string, object> metadata = null)
        {
            // NO-OP
            return Task.CompletedTask;
        }

        public Task RaiseErrorAsync(int errorCode, string errorMessage = null, string correlationKey = null, Dictionary<string, object> metadata = null)
        {
            // NO-OP
            return Task.CompletedTask;
        }

        public Task RaiseEventAsync(string eventName, string eventNameKey, IEnumerable<TelemetrySpan> associatedSpans = null, string correlationKey = null, Dictionary<string, object> attributes = null)
        {
            // NO-OP
            return Task.CompletedTask;
        }

        public Task RaiseWarningAsync(string message, IEnumerable<TelemetryWarningTuple> warnings, string correlationKey = null, Dictionary<string, object> metadata = null)
        {
            // NO-OP
            return Task.CompletedTask;
        }
    }
}
