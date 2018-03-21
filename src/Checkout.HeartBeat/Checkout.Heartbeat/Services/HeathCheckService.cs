using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics.Health;
using App.Metrics.Health.Builder;
using Checkout.Heartbeat.Models;

namespace Checkout.Heartbeat.Services
{
    public class HeathCheckService : HostedService, IHeathCheckService
    {
        private readonly IHealthRoot _health;
        private readonly HealthMonitorOptions _healthMonitorOptions;
        private readonly Action<HealthStatus> _onHealthCheck;
        private readonly TimeSpan _defaultTimeOut = TimeSpan.FromSeconds(10);
        private CancellationTokenSource _cancellationToken;

        public HeathCheckService(Action<HealthMonitorOptions> setupAction, Action<HealthStatus> onHealthCheck, List<HealthCheck> customHealthChecks = null)
        {
            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }
            
            //1. Assign an Action to be performed every time an healthcheck has been performed
            _onHealthCheck = onHealthCheck ?? throw new ArgumentNullException(nameof(onHealthCheck));

            _healthMonitorOptions = new HealthMonitorOptions();

            //2.  Configure Service
            setupAction?.Invoke(_healthMonitorOptions);
            
            var healthBuilder = new HealthBuilder();

            if (_healthMonitorOptions.PrivateMemorySizeLimit > 0)
            {
                healthBuilder.HealthChecks.AddProcessPrivateMemorySizeCheck("private_memory_size", _healthMonitorOptions.PrivateMemorySizeLimit);
            }

            if (_healthMonitorOptions.VirtualMemorySizeLimit > 0)
            {
                healthBuilder.HealthChecks.AddProcessVirtualMemorySizeCheck("virtual_memory_size", _healthMonitorOptions.VirtualMemorySizeLimit);
            }

            if (_healthMonitorOptions.PhysicalMemorySizeLimit > 0)
            {
                healthBuilder.HealthChecks.AddProcessPhysicalMemoryCheck("physical_memory_size", _healthMonitorOptions.PhysicalMemorySizeLimit);
            }

            if (_healthMonitorOptions.PingChecks?.Length > 0)
            {
                foreach (var pc in _healthMonitorOptions.PingChecks)
                {
                    healthBuilder.HealthChecks.AddPingCheck($"{pc.ToLowerInvariant()}_ping", pc, _healthMonitorOptions.Timeout ?? _defaultTimeOut);
                }
            }
            
            if (_healthMonitorOptions.HttpGetChecks?.Length > 0)
            {
                foreach (var hc in _healthMonitorOptions.HttpGetChecks)
                {
                    healthBuilder.HealthChecks.AddHttpGetCheck($"{hc.Host}_get", hc, _healthMonitorOptions.Timeout ?? _defaultTimeOut);
                }
            }

            //3. Add Project Custom HelathChecks
            customHealthChecks?.ForEach((x) => { healthBuilder.HealthChecks.AddCheck(x); });

            _health = healthBuilder.Build();
        }

        public Task StartAsync()
        {
            _cancellationToken = new CancellationTokenSource();
            _cancellationToken.CancelAfter(_healthMonitorOptions.Timeout ?? _defaultTimeOut);
            return StartAsync(_cancellationToken.Token);
        }
        
        public Task Stop()
        {
            return StopAsync(default(CancellationToken));
        }
        
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {   
            while (!cancellationToken.IsCancellationRequested)
            {
                var healthStatus = await _health.HealthCheckRunner.ReadAsync(cancellationToken);
                _onHealthCheck?.Invoke(healthStatus);

                //delay following execution
                await Task.Delay(_healthMonitorOptions.Frequency ?? TimeSpan.FromMinutes(5), cancellationToken);
            }
        }
    }
}