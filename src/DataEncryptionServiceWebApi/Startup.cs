using DataEncryptionService.Integration.MongoDB;
using DataEncryptionService.Integration.Vault;
using DataEncryptionService.WebApi.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Serilog;

namespace DataEncryptionService.WebApi
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the DI container.
        public void ConfigureServices(IServiceCollection services)
        {
            var serilogLogger = ConfigureLogger();
            services
                .AddMemoryCache()
                .AddLogging(builder =>
                {
                    builder.AddSimpleConsole(options =>
                    {
                        options.SingleLine = false;
                        options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffK ";
                    });
                    builder.AddSerilog(logger: serilogLogger, dispose: true);
                })
                .AddDataEncryptionServices("appsettings.json")
                .AddDataEncryptionServiceVaultIntegration()
                .AddDataEncryptionServiceMongoDbIntegration();

            services.AddSingleton<ISecurityVaultTokenHandler, SecurityVaultTokenHandler>();

            services.AddAuthentication(DataEncryptionServiceTokenAuthOptions.DefaultScemeName)
                        .AddScheme<DataEncryptionServiceTokenAuthOptions, DataEncryptionServiceTokenAuthHandler>(DataEncryptionServiceTokenAuthOptions.DefaultScemeName, opt => { });

            services.AddControllers()
                        .AddJsonOptions(options =>
                            {
                                options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "DataEncryptionServiceWebApi", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Data Encryption Service API v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private static Serilog.Core.Logger ConfigureLogger()
        {
            return new LoggerConfiguration()
                                    .MinimumLevel.Verbose()
                                    .Enrich.FromLogContext()
                                    .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
                                    .CreateLogger();
        }
    }
}