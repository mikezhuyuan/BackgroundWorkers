using System;
using System.Collections.Generic;
using System.Linq;

namespace BackgroundWorkers
{
    public class NewWorkItemDispatcherFactory : IPrepareWorkItemsFactory<NewWorkItem>
    {
        readonly WorkersConfiguration _configuration;

        public NewWorkItemDispatcherFactory(WorkersConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");
            _configuration = configuration;
        }

        public IPrepareWorkItems<NewWorkItem> Create()
        {
            var clients = new List<ISendMessage<Guid>>();

            var routeTable = new List<WorkItemRouteData>();

            foreach (var wiq in _configuration.WorkItemQueues)
            {
                var client = MsmqHelpers.CreateQueue<Guid>(wiq);
                routeTable.Add(new WorkItemRouteData {Client = client, Config = wiq});
                clients.Add(client);
            }

            
            var mergeClient = MsmqHelpers.CreateQueue<Guid>(_configuration.MergeableWorkItemQueue);
            clients.Add(mergeClient);

            var route = new WorkItemRoute(routeTable, mergeClient);

            return new NewWorkItemDispatcher(_configuration.NewWorkItemQueue.Name, 
                _configuration.MessageFormatter, _configuration.WorkItemRepositoryProvider,
                clients, route);
        }
    }
}