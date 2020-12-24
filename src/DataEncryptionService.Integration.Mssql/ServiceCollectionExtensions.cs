using DataEncryptionService.Integration.MSSQL.Storage;
using DataEncryptionService.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace DataEncryptionService.Integration.MSSQL
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDataEncryptionServiceMSSqlIntegration(this IServiceCollection services)
        {
            services.AddSingleton<IStorageProvider, MssqlDataStorage>();
            return services;
        }
    }
}
