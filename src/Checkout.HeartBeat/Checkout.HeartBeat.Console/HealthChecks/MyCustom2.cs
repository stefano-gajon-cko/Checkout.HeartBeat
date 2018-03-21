using System.Threading;
using System.Threading.Tasks;
using App.Metrics.Health;

namespace Checkout.HeartBeat.Console.HealthChecks
{
    public class MyCustom2 : HealthCheck
    {
        public MyCustom2() : base("MyCustom2- Health Check") { }

        protected override ValueTask<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return new ValueTask<HealthCheckResult>(HealthCheckResult.Healthy());
        }
    }
}
