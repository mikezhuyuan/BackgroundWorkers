using System;
using System.Threading.Tasks;
using System.Transactions;
using BackgroundWorkers.Persistence;

namespace BackgroundWorkers
{
    public class PoisonedWorkItemDispatcher : IHandleRawMessage<Guid>
    {
        readonly IWorkItemRepository _workItemRepository;
        readonly IDependencyScope _dependencyScope;
        readonly IMessageFormatter _formatter;
        readonly ILogger _logger;

        public PoisonedWorkItemDispatcher(
            IWorkItemRepository workItemRepository, 
            IDependencyScope dependencyScope, 
            IMessageFormatter formatter, 
            ILogger logger)
        {
            if (workItemRepository == null) throw new ArgumentNullException("workItemRepository");
            if (dependencyScope == null) throw new ArgumentNullException("dependencyScope");
            if (formatter == null) throw new ArgumentNullException("formatter");
            if (logger == null) throw new ArgumentNullException("logger");

            _workItemRepository = workItemRepository;
            _dependencyScope = dependencyScope;
            _formatter = formatter;
            _logger = logger;
        }

        object FindFaultHandler(object message)
        {
            var t = typeof (IHandleFault<>);
            var gt = t.MakeGenericType(message.GetType());

            object handler;
            _dependencyScope.TryGetService(gt, out handler);

            return handler;
        }

        public Task Run(Guid message)
        {
            try
            {
                _logger.Information("WI-{0} - Poisoned", message);

                var wi = _workItemRepository.Find(message);

                if (wi == null)
                {
                    _logger.Warning("WI-{0} - Could not be found in the repository.", message);
                    return null;
                }

                var body = _formatter.Deserialize(wi.Message);

                var handler = FindFaultHandler(body);

                if (handler != null)
                {
                    var method = handler.GetType().GetMethod("Run");
                    method.Invoke(handler, new[] { body });
                }

            }
            catch(Exception e)
            {
                if (e.IsFatal()) throw;

                _logger.Exception(e);
            }
            
            return null;
        }
    }
}