using System;

namespace BackgroundWorkers
{
    public class WorkItemDispatcherFactory : IHandleRawMessageFactory<Guid>
    {
        readonly WorkersConfiguration _configuration;

        public WorkItemDispatcherFactory(WorkersConfiguration configuration)

        {
            if (configuration == null) throw new ArgumentNullException("configuration");
            _configuration = configuration;
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
                _configuration.RetryCount
                );

            return new WorkItemDispatcher(
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