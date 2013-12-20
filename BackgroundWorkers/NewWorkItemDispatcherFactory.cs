using System;
using System.Linq;

namespace BackgroundWorkers
{
    public class NewWorkItemDispatcherFactory : IHandleRawMessageFactory<NewWorkItem>
    {
        readonly WorkersConfiguration _configuration;

        public NewWorkItemDispatcherFactory(WorkersConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");
            _configuration = configuration;
        }

        public IHandleRawMessage<NewWorkItem> Create()
        {
            var clients = _configuration.WorkItemQueues.Select(MsmqHelpers.CreateQueue<Guid>);

            return new NewWorkItemDispatcher(_configuration.MessageFormatter, _configuration.WorkItemRepositoryProvider,
                clients, _configuration.Logger);
        }
    }
}