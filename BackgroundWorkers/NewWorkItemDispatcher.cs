using System;
using System.Collections.Generic;
using System.Diagnostics;
using BackgroundWorkers.Persistence;

namespace BackgroundWorkers
{    
    public class NewWorkItemDispatcher : IPrepareWorkItems<NewWorkItem>, IDisposable
    {
        readonly IMessageFormatter _messageFormatter;
        readonly IWorkItemRepositoryProvider _workItemRepositoryProvider;
        readonly IEnumerable<ISendMessage<Guid>> _clients;
        readonly WorkItemRoute _workItemRoute;

        readonly PerformanceCounter _counter = new PerformanceCounter(
            PerformanceCounterConstants.Category,
            PerformanceCounterConstants.NewWorkItemsDispatcherThroughputCounter, 
            false);

        public NewWorkItemDispatcher(IMessageFormatter messageFormatter, 
            IWorkItemRepositoryProvider workItemRepositoryProvider, 
            IEnumerable<ISendMessage<Guid>> clients,
            WorkItemRoute workItemRoute)
        {
            if (messageFormatter == null) throw new ArgumentNullException("messageFormatter");
            if (workItemRepositoryProvider == null) throw new ArgumentNullException("workItemRepositoryProvider");
            if (clients == null) throw new ArgumentNullException("clients");
            if (workItemRoute == null) throw new ArgumentNullException("workItemRoute");

            _messageFormatter = messageFormatter;
            _workItemRepositoryProvider = workItemRepositoryProvider;
            _clients = clients;
            _workItemRoute = workItemRoute;
        }

        public IEnumerable<WorkItem> Prepare(NewWorkItem message)
        {
            using (var repository = _workItemRepositoryProvider.Create())
            {
                var wib = _messageFormatter.Deserialize(message.Body);

                foreach (var c in _workItemRoute.GetRouteTargets(wib.GetType()))
                {
                    var wi = new WorkItem(wib.GetType().FullName, message.Body, c.Queue, message.CreatedOn, message.ParentId);
                    repository.Add(wi);
                    c.Send(wi.Id);
                    yield return wi;
                }
            }

            _counter.Increment();
        }

        public void Dispose()
        {
            foreach(var c in _clients)
                c.Dispose();
        }
    }

    [Serializable]
    public class NewWorkItem
    {
        public string Body { get; set; }
        public DateTime CreatedOn { get; set; }

        public Guid? ParentId { get; set; }
    }
}