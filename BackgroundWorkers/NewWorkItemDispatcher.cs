using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BackgroundWorkers.Persistence;

namespace BackgroundWorkers
{    
    public class NewWorkItemDispatcher : IHandleRawMessage<NewWorkItem>
    {
        readonly IMessageFormatter _messageFormatter;
        readonly IWorkItemRepositoryProvider _workItemRepositoryProvider;
        readonly IEnumerable<ISendMessage<Guid>> _clients;
        readonly ILogger _logger;

        public NewWorkItemDispatcher(IMessageFormatter messageFormatter, 
            IWorkItemRepositoryProvider workItemRepositoryProvider, 
            IEnumerable<ISendMessage<Guid>> clients,
            ILogger logger)
        {
            if (messageFormatter == null) throw new ArgumentNullException("messageFormatter");
            if (workItemRepositoryProvider == null) throw new ArgumentNullException("workItemRepositoryProvider");
            if (clients == null) throw new ArgumentNullException("clients");
            if (logger == null) throw new ArgumentNullException("logger");

            _messageFormatter = messageFormatter;
            _workItemRepositoryProvider = workItemRepositoryProvider;
            _clients = clients;
            _logger = logger;
        }

        public Task Run(NewWorkItem message)
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
                        return Task.FromResult((object)null);
                    }
                }

                var wib = _messageFormatter.Deserialize(message.Body);

                foreach (var c in _clients)
                {
                    var wi = new WorkItem(wib.GetType().FullName, message.Body, c.Queue, message.CreatedOn, parent);
                    repository.Add(wi);
                    c.Send(wi.Id);
                }   
            }

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