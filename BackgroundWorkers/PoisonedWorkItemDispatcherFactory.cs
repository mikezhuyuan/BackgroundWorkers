using System;

namespace BackgroundWorkers
{
    public class PoisonedWorkItemDispatcherFactory : IHandleRawMessageFactory<Guid>
    {
        readonly Configuration _configuration;

        public PoisonedWorkItemDispatcherFactory(Configuration configuration)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");
            _configuration = configuration;
        }

        public IHandleRawMessage<Guid> Create()
        {
            return new PoisonedWorkItemDispatcher(_configuration.WorkItemRepositoryProvider,
                _configuration.DependencyResolver, _configuration.MessageFormatter, _configuration.Logger);
        }
    }
}