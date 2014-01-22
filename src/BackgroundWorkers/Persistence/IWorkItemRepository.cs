using System;
using System.Collections.Generic;

namespace BackgroundWorkers.Persistence
{
    public interface IWorkItemRepository : IDisposable
    {
        WorkItem Find(Guid workItemId);

        IEnumerable<WorkItem> FindAllByParentId(Guid parentWorkItemId);

        void Update(WorkItem workItem);

        void Add(WorkItem workItem);

        IEnumerable<WorkItem> ReadyToRetry(DateTime now);

        IEnumerable<WorkItem> IncompleteItems();
    }
}