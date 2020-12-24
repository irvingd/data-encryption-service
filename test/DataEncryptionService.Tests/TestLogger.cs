using Microsoft.Extensions.Logging;
using Serilog;

namespace DataDataEncryptionService.Tests
{
    public static class TestLogger
    {
        private static Microsoft.Extensions.Logging.LoggerFactory _loggerFactory;

        static TestLogger()
        {
            var serilogLogger = new LoggerConfiguration()
                                    .Enrich.FromLogContext()
                                    .WriteTo.Console()
                                    .CreateLogger();

            _loggerFactory = new Microsoft.Extensions.Logging.LoggerFactory();
            _loggerFactory.AddSerilog(serilogLogger);
        }

        public static Microsoft.Extensions.Logging.ILogger<T> GetLogger<T>()
        {
            return _loggerFactory.CreateLogger<T>();
        }
    }
}