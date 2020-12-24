using DataEncryptionService.Integration.MongoDB.Storage;
using DataEncryptionService.Integration.MongoDB.Telemetry;
using DataEncryptionService.Storage;
using DataEncryptionService.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace DataEncryptionService.Integration.MongoDB
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDataEncryptionServiceMongoDbIntegration(this IServiceCollection services)
        {
            services.AddSingleton<ITelemetrySink, MongoDbSink>();
            services.AddSingleton<IStorageProvider, MongoDbDataStorage>();

            return services;
        }
    }
}
