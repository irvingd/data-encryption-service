using System.Threading.Tasks;

namespace DataEncryptionService.Telemetry
{
    public interface ITelemetrySink
    {
        string Name { get; }
        Task CommitEvent(TelemetryEvent eventData);
    }
}
