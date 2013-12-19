using System.Threading.Tasks;

namespace BackgroundWorkers
{
    public interface IHandleRawMessage<in T>
    {
        Task Run(T message);
    }
}