using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using App.Metrics.Health;
using Checkout.Heartbeat.Runners;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;

namespace Checkout.HeartBeat.Console
{
    class Program
    {
        private static ILogger _logger;
        private static IConfiguration _configuration;

        static void Main(string[] args)
        {
            //1. Configuartion  
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            //2. Logger
            _logger = new LoggerConfiguration()
                .ReadFrom.Configuration(_configuration)
                .CreateLogger()
                .ForContext<Program>();
            
            #region Runner Configuration - Programmatic
           
            var runner = new HealthMonitorRunner()
                .Configure(options =>
                {
                    options.Frequency = TimeSpan.FromSeconds(3);
                    options.Timeout = TimeSpan.FromSeconds(3);
                    options.PrivateMemorySizeLimit = 100; //bytes
                    options.VirtualMemorySizeLimit = 200; //bytes
                    options.PhysicalMemorySizeLimit = 300; //bytes
                    options.PingChecks = new[]
                    {
                        "google.com",
                        "checkout.com"
                    };
                    options.HttpGetChecks = new[]
                    {
                        new Uri("https://github.com/"),
                        new Uri("https://checkout.com/")
                    };
                })
                .OnHealthStatus(OnHealthStatus)
                .Start();

            #endregion

            #region Runner Configuration - By Configuration
            
            //var runner = new HealthMonitorRunner()
            //    .ConfigureFrom(_configuration)
            //    .OnHealthStatus(status => 
            //    {
            //        _logger.Information(JsonConvert.SerializeObject(status, Formatting.Indented));
            //    })
            //    .Start();

            #endregion

            //4. Sample Code to demo that monitor is not a blocking task
            while (true)
            {
                _logger.Information("Doing somehting else...");
                Task.Delay(TimeSpan.FromSeconds(1)).Wait();
            }
        }

        private static void OnHealthStatus(HealthStatus status)
        {
            var msg = $"Application Health Status: {status.Status.ToString().ToUpper()} due to [ {string.Join(" | ", status.Results.Select(r => $"{r.Name} > {r.Check.Status} > {r.Check.Message}"))} ]";

            switch (status.Status)
            {
                case HealthCheckStatus.Healthy:
                    _logger.Information(msg);
                    break;
                case HealthCheckStatus.Degraded:
                    _logger.Warning(msg);
                    break;
                case HealthCheckStatus.Unhealthy:
                    _logger.Error(msg);
                    break;
                default:
                    _logger.Warning(msg);
                    break;
            }
        }

    }
}

