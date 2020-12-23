using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataEncryptionService.Configuration;
using DataEncryptionService.Telemetry;
using Microsoft.Extensions.Logging;

namespace DataEncryptionService.Core.Telemetry
{
    public class TelemetrySourceClient : ITelemetrySourceClient
    {
        private readonly ILogger _log;
        private readonly string _sourceName;
        private readonly ICollection<ITelemetrySink> _allSinks;

        public TelemetrySourceClient(TelemetryConfiguration config, IEnumerable<ITelemetrySink> allSinks, ILogger<TelemetrySourceClient> log)
        {
            _log = log;
            _sourceName = config.AppSourceName;

            // Use only the sinks that have been requested in the configuration of the host application
            var configuredSinks = new HashSet<ITelemetrySink>();
            foreach(var sink in allSinks)
            {
                var key = config.Sinks?.Keys.FirstOrDefault(p => p.ToLower() == sink.Name.ToLower());
                if (key != null)
                {
                    _log.LogInformation($"Telemetry sink target: {sink.Name}");
                    configuredSinks.Add(sink);
                }
            }

            _allSinks = configuredSinks;
        }

        public Task RaiseEventAsync(string eventName, string eventNameKey, IEnumerable<TelemetrySpan> associatedSpans = null, string correlationKey = null, Dictionary<string, object> metadata = null)
        {
            return RaiseEventFromDataAsync(eventName, eventNameKey, TelemetryCategory.Activity.ToString(), associatedSpans, correlationKey, metadata);
        }

        public Task RaiseErrorAsync(int errorCode, string errorMessage = null, string correlationKey = null, Dictionary<string, object> metadata = null)
        {
            if (null == metadata)
            {
                metadata = new Dictionary<string, object>();
            }

            metadata.Remove(TelemetryMetadataProperty.Error.ToString());
            metadata.Remove(TelemetryMetadataProperty.Message.ToString());

            metadata.Add(TelemetryMetadataProperty.Error.ToString(), errorCode);
            metadata.Add(TelemetryMetadataProperty.Message.ToString(), errorMessage);

            return RaiseEventFromDataAsync(TelemetryCategory.Error.ToString(), "-1", TelemetryCategory.Error.ToString(), associatedSpans: null, correlationKey, metadata);
        }

        public Task RaiseErrorAsync(Exception exception, string errorMessage = null, string correlationKey = null, Dictionary<string, object> metadata = null)
        {
            if (null == metadata)
            {
                metadata = new Dictionary<string, object>();
            }
            
            metadata.Remove(TelemetryMetadataProperty.Message.ToString());
            metadata.Remove(TelemetryMetadataProperty.Exception.ToString());

            metadata.Add(TelemetryMetadataProperty.Message.ToString(), exception.Message);
            metadata.Add(TelemetryMetadataProperty.Exception.ToString(), exception.ToString());

            return RaiseEventFromDataAsync(TelemetryCategory.Error.ToString(), "-1", TelemetryCategory.Error.ToString(), associatedSpans: null, correlationKey, metadata);
        }

        public Task RaiseWarningAsync(string message, IEnumerable<TelemetryWarningTuple> warnings = null, string correlationKey = null, Dictionary<string, object> metadata = null)
        {
            if (null == metadata)
            {
                metadata = new Dictionary<string, object>();
            }

            metadata.Remove(TelemetryMetadataProperty.Message.ToString());
            metadata.Add(TelemetryMetadataProperty.Message.ToString(), message);

            if (warnings?.Count() > 0)
            {
                metadata.Remove(TelemetryMetadataProperty.Warnings.ToString());
                metadata.Add(TelemetryMetadataProperty.Warnings.ToString(), warnings);
            }

            return RaiseEventFromDataAsync(TelemetryCategory.Warning.ToString(), "-2", TelemetryCategory.Warning.ToString(), associatedSpans: null, correlationKey, metadata);
        }

        private Task RaiseEventFromDataAsync(string eventName, string eventNameKey, string category, IEnumerable<TelemetrySpan> associatedSpans = null, string correlationKey = null, Dictionary<string, object> metadata = null)
        {
            try
            {
                var eventData = new TelemetryEvent()
                {
                    EventName = eventName,
                    EventNameKey = eventNameKey,
                    CorrelationKey = correlationKey,
                    Category = category,
                    Source = _sourceName,
                    SourceHostName = Environment.MachineName  // TODO: should we get the DNS name? What's the value of this on a Linux container?
                };

                if (associatedSpans?.Count() > 0)
                {
                    eventData.Spans = new List<TelemetrySpan>(associatedSpans);
                }

                if (metadata?.Count > 0)
                {
                    eventData.Metadata = new Dictionary<string, object>(metadata);
                }

                return SinkEventData(eventData);
            }
            catch (Exception e)
            {
                _log.LogError(e, "Failed to raise application event");
                return Task.CompletedTask;
            }
        }

        private Task SinkEventData(TelemetryEvent eventData)
        {
            List<Task> tasks = new List<Task>(_allSinks.Count);
            foreach (var sink in _allSinks)
            {
                tasks.Add(sink.CommitEvent(eventData));
            }

            return Task.WhenAll(tasks);
        }
    }
}