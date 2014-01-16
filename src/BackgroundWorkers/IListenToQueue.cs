using System.Threading.Tasks;

namespace BackgroundWorkers
{
    public interface IListenToQueue
    {
        Task Start();
    }
}