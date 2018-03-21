using System;

namespace Checkout.Heartbeat.Models
{
    public class HealthMonitorOptions
    {
        public TimeSpan? Frequency { get; set; }
        public TimeSpan? Timeout { get; set; }
        public long PrivateMemorySizeLimit { get; set; }
        public long VirtualMemorySizeLimit { get; set; }
        public long PhysicalMemorySizeLimit { get; set; }
        public string[] PingChecks { get; set; }
        public Uri[] HttpGetChecks { get; set; }
    }
}