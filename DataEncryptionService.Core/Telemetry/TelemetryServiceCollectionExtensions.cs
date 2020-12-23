using DataEncryptionService.Core.Telemetry;
using DataEncryptionService.Core.Telemetry.Sinks;
using Microsoft.Extensions.DependencyInjection;

namespace DataEncryptionService.Telemetry
{
    public static class TelemetryServiceCollectionExtensions
    {
        public static IServiceCollection AddTelemetryClient(this IServiceCollection services)
        {
            return services.AddSingleton<ITelemetrySourceClient, TelemetrySourceClient>();
        }

        public static IServiceCollection AddDataEncryptionServiceTelemetrySinkConsole(this IServiceCollection services)
        {
            return services.AddSingleton<ITelemetrySink, ConsoleSink>();
        }
    }
}
