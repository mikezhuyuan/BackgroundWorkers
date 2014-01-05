﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using BackgroundWorkers.Persistence;

namespace BackgroundWorkers
{    
    public class NewWorkItemDispatcher : IHandleRawMessage<NewWorkItem>
    {
        readonly IMessageFormatter _messageFormatter;
        readonly IWorkItemRepositoryProvider _workItemRepositoryProvider;
        readonly IEnumerable<ISendMessage<Guid>> _clients;
        readonly WorkItemRoute _workItemRoute;
        readonly ILogger _logger;

        readonly PerformanceCounter _counter = new PerformanceCounter(
            PerformanceCounterConstants.Category,
            PerformanceCounterConstants.NewWorkItemsDispatcherThroughputCounter, 
            false);

        public NewWorkItemDispatcher(IMessageFormatter messageFormatter, 
            IWorkItemRepositoryProvider workItemRepositoryProvider, 
            IEnumerable<ISendMessage<Guid>> clients,
            WorkItemRoute workItemRoute,
            ILogger logger)
        {
            if (messageFormatter == null) throw new ArgumentNullException("messageFormatter");
            if (workItemRepositoryProvider == null) throw new ArgumentNullException("workItemRepositoryProvider");
            if (clients == null) throw new ArgumentNullException("clients");
            if (workItemRoute == null) throw new ArgumentNullException("workItemRoute");
            if (logger == null) throw new ArgumentNullException("logger");

            _messageFormatter = messageFormatter;
            _workItemRepositoryProvider = workItemRepositoryProvider;
            _clients = clients;
            _workItemRoute = workItemRoute;
            _logger = logger;
        }

        public void OnDequeue(NewWorkItem message)
        {
            WorkItem parent = null;

            using (var repository = _workItemRepositoryProvider.Create())
            {
                if (message.ParentId.HasValue)
                {
                    parent = repository.Find(message.ParentId.Value);
                    if (parent == null)
                    {
                        _logger.Warning("Could not find the parent work item - {0}. New work item creation failed.", message.ParentId);
                        return;
                    }
                }

                var wib = _messageFormatter.Deserialize(message.Body);

                foreach (var c in _workItemRoute.GetRouteTargets(wib.GetType()))
                {
                    var wi = new WorkItem(wib.GetType().FullName, message.Body, c.Queue, message.CreatedOn, parent);
                    repository.Add(wi);
                    c.Send(wi.Id);
                }
            }

            _counter.Increment();
        }

        public Task Run(NewWorkItem message)
        {         
            return Task.FromResult((object)null);            
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