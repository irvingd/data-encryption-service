using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DataEncryptionService.Telemetry;

namespace DataEncryptionService.Core.Telemetry.Sinks
{
    public class ConsoleSink : ITelemetrySink
    {
        private readonly JsonSerializerOptions _defaultJsonSerializationOptions = new JsonSerializerOptions() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = true };

        public string Name => "Console";

        public Task CommitEvent(TelemetryEvent eventData)
        {
            string json = JsonSerializer.Serialize(eventData, _defaultJsonSerializationOptions);
            OutputLine($"*** Telemetry Event: {eventData.EventName} {GetEllapsedTimeLabel(eventData.Spans)}\r\n{json}");

            return Task.CompletedTask;
        }

        private void OutputLine(string outputLine)
        {
            System.Console.Out.WriteLine(outputLine);
        }

        static public string GetEllapsedTimeLabel(List<TelemetrySpan> spans)
        {
            TelemetrySpan span = spans?.Find(p => p.NestLevel == 1);
            if (null == span)
            {
                return null;
            }

            return $"(Duration: {span.ElapsedMs} ms)";

            //TimeSpan ts = span.EndedOn - span.StartedOn;
            //var sb = new StringBuilder("(Duration: ");
            //if (span.EllapsedMs.TotalMilliseconds < 1.0)
            //{
            //    sb.Append($"{ts.TotalMilliseconds} ms");
            //}
            //else if (ts.TotalSeconds < 1400)
            //{
            //    sb.Append($"{ts.TotalMilliseconds} sec");
            //}
            //else
            //{
            //    sb.Append(ts);
            //}
            //sb.Append(")");
            //return sb.ToString();
        }
    }
}