using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BackgroundWorkers.Persistence;

namespace BackgroundWorkers
{    
    public class WorkItemQueue : IHandleRawMessage<NewWorkItem>
    {
        readonly IMessageFormatter _messageFormatter;
        readonly IWorkItemRepository _workItemRepository;
        readonly IEnumerable<IMessageQueue<Guid>> _clients;
        readonly ILogger _logger;

        public WorkItemQueue(IMessageFormatter messageFormatter, 
            IWorkItemRepository workItemRepository, 
            IEnumerable<IMessageQueue<Guid>> clients,
            ILogger logger)
        {
            if (messageFormatter == null) throw new ArgumentNullException("messageFormatter");
            if (workItemRepository == null) throw new ArgumentNullException("workItemRepository");
            if (clients == null) throw new ArgumentNullException("clients");
            if (logger == null) throw new ArgumentNullException("logger");

            _messageFormatter = messageFormatter;
            _workItemRepository = workItemRepository;
            _clients = clients;
            _logger = logger;
        }

        public Task Run(NewWorkItem message)
        {
            WorkItem parent = null;

            if (message.ParentId.HasValue)
            {
                parent = _workItemRepository.Find(message.ParentId.Value);
                if (parent == null)
                {
                    _logger.Warning("Could not find the parent work item - {0}. New work item creation failed.", message.ParentId);
                    return null;
                }
            }

            var wib = _messageFormatter.Deserialize(message.Body);

            foreach (var c in _clients)
            {
                var wi = new WorkItem(wib.GetType().FullName, message.Body, c.Queue, message.CreatedOn, parent);
                _workItemRepository.Add(wi);
                c.Send(wi.Id);
            }

            return null;
        }
    }

    public class NewWorkItem
    {
        public string Body { get; set; }
        public DateTime CreatedOn { get; set; }

        public Guid? ParentId { get; set; }
    }
}