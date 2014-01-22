using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace BackgroundWorkers.Persistence
{
    public class InMemoryWorkItemRepository : IWorkItemRepository
    {
        static readonly ConcurrentDictionary<Guid, WorkItem> Items = new ConcurrentDictionary<Guid, WorkItem>();

        public WorkItem Find(Guid workItemId)
        {
            WorkItem item;
            Items.TryGetValue(workItemId, out item);
            return item;
        }

        public IEnumerable<WorkItem> FindAllByParentId(Guid parentWorkItemId)
        {
            return Items.Values.Where(i => i.ParentId == parentWorkItemId);
        }

        public void Update(WorkItem workItem)
        {
        }

        public void Add(WorkItem workItem)
        {
            Items.AddOrUpdate(workItem.Id, workItem, (id, wi) => workItem);
        }

        public IEnumerable<WorkItem> ReadyToRetry(DateTime now)
        {

            return Items.Where(i => i.Value.RetryOn <= now).Select(i => i.Value);
        }

        public IEnumerable<WorkItem> IncompleteItems()
        {
            return Items.Where(i => i.Value.Status <= WorkItemStatus.Running).Select(i => i.Value);
        }

        public void Dispose()
        {            
        }
    }
}