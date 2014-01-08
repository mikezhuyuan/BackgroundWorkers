using System;

namespace BackgroundWorkers
{
    public class PoisonedWorkItemDispatcherFactory : IPrepareWorkItemsFactory<Guid>
    {
        readonly WorkersConfiguration _configuration;

        public PoisonedWorkItemDispatcherFactory(WorkersConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");
            _configuration = configuration;
        }

        public IPrepareWorkItems<Guid> Create()
        {
            return new PoisonedWorkItemDispatcher(_configuration.WorkItemRepositoryProvider,
                _configuration.DependencyResolver, _configuration.MessageFormatter, _configuration.Logger);
        }
    }
}