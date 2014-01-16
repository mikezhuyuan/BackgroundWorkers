using System;

namespace BackgroundWorkers
{
    public interface IDependencyResolver : IDisposable
    {
        IDependencyScope BeginScope();
    }
}