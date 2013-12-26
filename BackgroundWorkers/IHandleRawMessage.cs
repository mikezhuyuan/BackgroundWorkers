using System;
using System.Threading.Tasks;

namespace BackgroundWorkers
{
    public interface IHandleRawMessage<in T> : IDisposable
    {
        void OnDequeue(T message);

        Task Run(T message);
    }
}