using System;
using System.Threading.Tasks;

namespace BackgroundWorkers
{
    public interface IHandleRawMessage<in T> : IDisposable
    {
        Task<Task> Run(T message);
    }
}