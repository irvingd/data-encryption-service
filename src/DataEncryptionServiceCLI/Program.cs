using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using DataEncryptionService.CLI.Telemetry.Names;
using DataEncryptionService.CLI.ToolActions;
using DataEncryptionService.Integration.MongoDB;
using DataEncryptionService.Integration.Vault;
using DataEncryptionService.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace DataEncryptionService.CLI
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var parser = new Parser(settings => {
                settings.CaseInsensitiveEnumValues = true;
                settings.CaseSensitive = false;
            });

            await parser.ParseArguments<RuntimeOptions>(args)
                .WithNotParsed(errs => ShowArgumentErrors(errs))
                .WithParsedAsync(options => RunApplication(options));
        }

        static public async Task RunApplication(RuntimeOptions options)
        {
            // Setup DI
            IServiceProvider services = ConfigureServices();
            
            var log = services.GetRequiredService<ILogger<Program>>();

            // Get the first runner that can run the specified action (there should only be one)
            IToolAction runner = services.GetServices<IToolAction>()
                                            .Where(s => s.Action == options.Action)
                                            .FirstOrDefault();
            if (runner is null)
            {
                string message = $"Action [{options.Action}] not implemented.";
                log.LogError(message);
                throw new NotImplementedException(message);
            }

            if (!runner.HasValidConfiguration())
            {
                log.LogError($"The configuration for tool action runner [{runner.Name}] is missing or invalid.");
                return;
            }

            if (!runner.HasValidRunOptions(options))
            {
                log.LogError($"The selected action [{runner.Name}] is missing additional command line parameters.");
                return;
            }

            log.LogInformation("<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");
            log.LogInformation($"Starting {runner.Name}....");
            var spans = new List<TelemetrySpan>();
            using (SpanMeasure.Start(SpanName.Toolaction_Execution_Time, spans))
            {
                await runner.ExecuteActionAsync(options);
            }
            var ellapsed = new TimeSpan(spans[0].ElapsedTicks);
            log.LogInformation($"{runner.Name} finished - Elapsed Time: {ellapsed}");
            log.LogInformation(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
        }

        static public IServiceProvider ConfigureServices()
        {
            // Setup the logger
            var serilogLogger = ConfigureLogger();

            // Setup the container
            var services = new ServiceCollection();
            services
                .AddLogging(builder => {
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

            // Register all supported tool actions
            services.AddSingleton<IToolAction, RotateKeyToolAction>();
            services.AddSingleton<IToolAction, Generate3desKeyToolAction>();
            services.AddSingleton<IToolAction, GenerateAesKeyToolAction>();

            // Container is ready
            return services.BuildServiceProvider();
        }

        static public int ShowArgumentErrors(IEnumerable<Error> errs)
        {
            int exitCode = -2;
            Console.WriteLine($"Errors {errs.Count()}");
            if (errs.Any(x => x is HelpRequestedError || x is VersionRequestedError))
            {
                exitCode = -1;
            }
            Console.WriteLine($"Exit Code: {exitCode}");
            return exitCode;
        }

        private static Serilog.Core.Logger ConfigureLogger()
        {
            string logFilePath = Path.Combine(Environment.CurrentDirectory, "log.txt");
            return new LoggerConfiguration()
                                    .MinimumLevel.Verbose()
                                    .Enrich.FromLogContext()
                                    .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                                    .CreateLogger();
        }
    }
}
