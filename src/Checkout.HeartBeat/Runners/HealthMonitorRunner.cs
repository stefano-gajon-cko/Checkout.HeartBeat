using System;
using System.Linq;
using App.Metrics.Health;
using Checkout.Heartbeat.Models;
using Checkout.Heartbeat.Services;
using Microsoft.Extensions.Configuration;
using StructureMap;

namespace Checkout.Heartbeat.Runners
{
    /// <summary>
    /// Runner for Console Application Health Monitor
    /// </summary>
    public class HealthMonitorRunner
    {
        private IContainer _container;
        private IHeathCheckService _service;
        private Action<HealthStatus> _onReport;
        private Action<HealthMonitorOptions> _setupAction;
        private const string DefaultSectionName = "HealthCheckMonitor";
        private const double DefaultFrequency = 10000;
        private const double DefaultTimeout = 10000;

        public HealthMonitorRunner()
        {
            _container = new Container();
        }

        public HealthMonitorRunner(IContainer container)
        {
            _container = container;
        }

        /// <summary>
        /// Provide a way to configure the monitor trough HealthMonitorOptions parameter
        /// </summary>
        public HealthMonitorRunner Configure(Action<HealthMonitorOptions> setupAction)
        {
            _setupAction = setupAction;

            _container.Configure(_ =>
            {
                _.Scan(s =>
                {
                    //Register all custom healthcheck implementation
                    s.AssembliesFromApplicationBaseDirectory();
                    s.AddAllTypesOf<HealthCheck>();
                });
            });

            return this;
        }

        /// <summary>
        /// Provide a way to configure the monitor trough configuration file
        /// </summary>
        public HealthMonitorRunner ConfigureFrom(IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));


            var configSection = configuration.GetSection(DefaultSectionName);

            double.TryParse(configSection["Timeout"], out var timeOut);
            double.TryParse(configSection["Frequency"], out var frequency);
            long.TryParse(configSection["PrivateMemorySizeLimit"], out var privateMemorySizeLimit);
            long.TryParse(configSection["VirtualMemorySizeLimit"], out var virtualMemorySizeLimit);
            long.TryParse(configSection["PhysicalMemorySizeLimit"], out var physicalMemorySizeLimit);
            var pingChecks = configSection.GetSection("PingChecks")?.GetChildren();
            var httpGetChecks = configSection.GetSection("HttpGetChecks")?.GetChildren();

            Configure(options =>
            {
                options.Timeout = TimeSpan.FromMilliseconds(timeOut > 0 ? timeOut : DefaultTimeout);
                options.Frequency = TimeSpan.FromMilliseconds(timeOut > 0 ? frequency : DefaultFrequency);
                options.PrivateMemorySizeLimit = privateMemorySizeLimit;
                options.VirtualMemorySizeLimit = virtualMemorySizeLimit;
                options.PhysicalMemorySizeLimit = physicalMemorySizeLimit;
                options.PingChecks = pingChecks?.Select(x => x.Value).ToArray();
                options.HttpGetChecks = httpGetChecks?.Select(x => new Uri(x.Value)).ToArray();
            });

            return this;
        }

        /// <summary>
        /// Specify the custom action to be perfomed whne an healthcheck has been reported
        /// </summary>
        public HealthMonitorRunner OnHealthStatus(Action<HealthStatus> action)
        {
            _onReport = action ?? throw new ArgumentNullException(nameof(action));
            return this;
        }

        /// <summary>
        /// Start the monitor
        /// </summary>
        public HealthMonitorRunner Start()
        {
            _service = new HeathCheckService(_setupAction, _onReport, _container.GetAllInstances<HealthCheck>()?.ToList());
         
            //Start retrieving health metrics
            _service
                .StartAsync()
                .Wait();

            return this;
        }

        /// <summary>
        /// Stop the monitor
        /// </summary>
        public HealthMonitorRunner Stop()
        {
            //Stop retrieving health metrics
            _service.Stop();
            return this;
        }
    }
}
