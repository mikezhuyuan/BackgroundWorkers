using System;
using System.Collections.Generic;

namespace BackgroundWorkers
{
    public interface IDependencyScope : IDisposable
    {
        object GetService(Type type);
        IEnumerable<object> GetServices(Type type);

        bool TryGetService(Type type, out object service);
    }
}