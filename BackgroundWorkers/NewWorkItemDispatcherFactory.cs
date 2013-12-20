using System;
using System.Linq;

namespace BackgroundWorkers
{
    public class NewWorkItemDispatcherFactory : IHandleRawMessageFactory<NewWorkItem>
    {
        readonly Configuration _configuration;

        public NewWorkItemDispatcherFactory(Configuration configuration)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");
            _configuration = configuration;
        }

        public IHandleRawMessage<NewWorkItem> Create()
        {
            var clients = _configuration.Queues.Select(MsmqHelpers.CreateQueue<Guid>);

            return new NewWorkItemDispatcher(_configuration.MessageFormatter, _configuration.WorkItemRepositoryProvider,
                clients, _configuration.Logger);
        }
    }
}