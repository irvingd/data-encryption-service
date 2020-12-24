using DataEncryptionService.Integration.MySql.Storage;
using DataEncryptionService.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace DataEncryptionService.Integration.MySql
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDataEncryptionServiceMySqlIntegration(this IServiceCollection services)
        {
            services.AddSingleton<IStorageProvider, MySqlDataStorage>();
            return services;
        }
    }
}
