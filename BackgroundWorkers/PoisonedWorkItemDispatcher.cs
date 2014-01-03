﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BackgroundWorkers.Persistence;

namespace BackgroundWorkers
{
    public class PoisonedWorkItemDispatcher : IHandleRawMessage<Guid>
    {
        readonly IWorkItemRepositoryProvider _workItemRepositoryProvider;
        readonly IDependencyResolver _dependencyResolver;
        readonly IMessageFormatter _formatter;
        readonly ILogger _logger;

        readonly PerformanceCounter _counter = new PerformanceCounter(
            PerformanceCounterConstants.Category,
            PerformanceCounterConstants.PoisonedWorkItemsDispatcherThroughputCounter,
            false
            );

        public PoisonedWorkItemDispatcher(
            IWorkItemRepositoryProvider workItemRepositoryProvider, 
            IDependencyResolver dependencyResolver, 
            IMessageFormatter formatter, 
            ILogger logger)
        {
            if (workItemRepositoryProvider == null) throw new ArgumentNullException("workItemRepositoryProvider");
            if (dependencyResolver == null) throw new ArgumentNullException("dependencyResolver");
            if (formatter == null) throw new ArgumentNullException("formatter");
            if (logger == null) throw new ArgumentNullException("logger");

            _workItemRepositoryProvider = workItemRepositoryProvider;
            _dependencyResolver = dependencyResolver;
            _formatter = formatter;
            _logger = logger;
        }

        object FindFaultHandler(IDependencyScope scope, object message)
        {
            var t = typeof (IHandleFault<>);
            var gt = t.MakeGenericType(message.GetType());

            object handler;
            scope.TryGetService(gt, out handler);

            return handler;
        }

        public void OnDequeue(Guid message)
        {
            _logger.Information("WI-{0} - Poisoned", message);

            using (var scope = _dependencyResolver.BeginScope())
            using (var repository = _workItemRepositoryProvider.Create())
            {
                var wi = repository.Find(message);

                if (wi == null)
                {
                    _logger.Warning("WI-{0} - Could not be found in the repository.", message);
                    return;
                }

                var body = _formatter.Deserialize(wi.Message);

                var handler = FindFaultHandler(scope, body);

                if (handler != null)
                {
                    var method = handler.GetType().GetMethod("Run");
                    method.Invoke(handler, new[] { body });
                }
            }

            _counter.Increment();

        }

        public Task Run(Guid message)
        {            
            return Task.FromResult((object)null);
        }

        public void Dispose()
        {
            
        }
    }
}