using System;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics.Health;

namespace Checkout.HeartBeat.Console.HealthChecks
{
     public class MyCustom1 : HealthCheck
     {
         public MyCustom1() : base("MyCustom1 - Health Check") { }

         protected override ValueTask<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default(CancellationToken))
         {
             if (DateTime.UtcNow.Second <= 20)
             {
                 return new ValueTask<HealthCheckResult>(HealthCheckResult.Degraded());
             }

             if (DateTime.UtcNow.Second >= 40)
             {
                 return new ValueTask<HealthCheckResult>(HealthCheckResult.Unhealthy());
             }

             return new ValueTask<HealthCheckResult>(HealthCheckResult.Healthy());
             
         }
     }
}
