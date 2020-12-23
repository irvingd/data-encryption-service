using DataEncryptionService.Integration.Postgresql.Storage;
using DataEncryptionService.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace DataEncryptionService.Integration.Postgresql
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDataEncryptionServicePostgresqlIntegration(this IServiceCollection services)
        {
            services.AddSingleton<IStorageProvider, PostgresqlDataStorage>();
            return services;
        }
    }
}
