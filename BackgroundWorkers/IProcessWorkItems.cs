using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BackgroundWorkers
{
    public interface IPrepareWorkItems<in T> : IDisposable
    {
        IEnumerable<WorkItem> Prepare(T message);
    }

    public interface IProcessWorkItems
    {
        Task Process(IEnumerable<WorkItem> workItems);
    }
}