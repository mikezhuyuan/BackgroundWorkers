using System;

namespace BackgroundWorkers
{
    public class WorkItemDispatcherFactory : IHandleRawMessageFactory<Guid>
    {
        readonly WorkersConfiguration _configuration;
        readonly QueueConfiguration _queueConfiguration;

        public WorkItemDispatcherFactory(WorkersConfiguration configuration, QueueConfiguration queueConfiguration)

        {
            if (configuration == null) throw new ArgumentNullException("configuration");
            if (queueConfiguration == null) throw new ArgumentNullException("queueConfiguration");

            _configuration = configuration;
            _queueConfiguration = queueConfiguration;
        }

        public IHandleRawMessage<Guid> Create()
        {
            var client = new WorkItemQueueClient(MsmqHelpers.CreateQueue<NewWorkItem>(_configuration.NewWorkItemQueue), _configuration.MessageFormatter,
                _configuration.Now);

            var errorHandlingPolicy = new ErrorHandlingPolicy(
                MsmqHelpers.CreateQueue<Guid>(_configuration.PoisonedWorkItemQueue),
                _configuration.WorkItemRepositoryProvider,
                _configuration.Now,
                _configuration.Logger,
                _queueConfiguration.RetryCount,
                _queueConfiguration.RetryDelay
                );

            return new WorkItemDispatcher(
                _queueConfiguration.Name,
                _configuration.DependencyResolver,
                _configuration.WorkItemRepositoryProvider,
                client,
                _configuration.MessageFormatter,
                errorHandlingPolicy,
                _configuration.Logger
                );
        }
    }
}