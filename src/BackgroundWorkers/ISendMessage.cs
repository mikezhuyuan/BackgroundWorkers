using System;

namespace BackgroundWorkers
{
    public interface ISendMessage<in T> : IDisposable
    {
        void Send(T message);
        string Queue { get; }
    }
}