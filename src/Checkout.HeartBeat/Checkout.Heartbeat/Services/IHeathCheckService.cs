using System.Threading.Tasks;

namespace Checkout.Heartbeat.Services
{
    public interface IHeathCheckService
    {
        Task StartAsync();
        Task Stop();
    }
}