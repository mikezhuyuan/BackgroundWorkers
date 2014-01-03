using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Transactions;
using BackgroundWorkers.Persistence;

namespace BackgroundWorkers
{
    public class WorkItemDispatcher : IHandleRawMessage<Guid>
    {
        readonly IDependencyResolver _dependencyResolver;
        readonly IWorkItemRepositoryProvider _workItemRepoitoryProvider;
        readonly IInternalWorkItemQueueClient _workItemQueueClient;
        readonly IMessageFormatter _messageFormatter;
        readonly IErrorHandlingPolicy _errorHandlingPolicy;
        readonly ILogger _logger;
        
        public WorkItemDispatcher(
            IDependencyResolver dependencyResolver,                        
            IWorkItemRepositoryProvider workItemRepoitoryProvider,
            IInternalWorkItemQueueClient workItemQueueClient,
            IMessageFormatter messageFormatter,
            IErrorHandlingPolicy errorHandlingPolicy,
            ILogger logger)
        {
            if (dependencyResolver == null) throw new ArgumentNullException("dependencyResolver");
            if (workItemRepoitoryProvider == null) throw new ArgumentNullException("workItemRepoitoryProvider");
            if (workItemQueueClient == null) throw new ArgumentNullException("workItemQueueClient");
            if (messageFormatter == null) throw new ArgumentNullException("messageFormatter");
            if (errorHandlingPolicy == null) throw new ArgumentNullException("errorHandlingPolicy");
            if (logger == null) throw new ArgumentNullException("logger");
            
            _dependencyResolver = dependencyResolver;
            _workItemRepoitoryProvider = workItemRepoitoryProvider;
            _workItemQueueClient = workItemQueueClient;
            _messageFormatter = messageFormatter;
            _errorHandlingPolicy = errorHandlingPolicy;
            _logger = logger;
        }

        public void OnDequeue(Guid message)
        {
            _logger.Information("WI-{0} - Dispatching", message);

            using (var repository = _workItemRepoitoryProvider.Create())
            {
                var workItem = repository.Find(message);

                if (workItem == null)
                {
                    _logger.Warning("WI-{0} - Could not be found in the repository.", message);
                    return;
                }

                if (!workItem.Running())
                {
                    _logger.Information("WI did not run {0}", workItem);
                    return;
                }

                repository.Update(workItem);
            }

        }

        public Task Run(Guid message)
        {           
            WorkItem workItem;

            using (var repository = _workItemRepoitoryProvider.Create())
            {
                workItem = repository.Find(message);
            }

            // Schedule the handler as a new task because we don't want the code in handler to
            // block the dispatcher.
            var t = Task.Run<Task>(() => DispatchCore(workItem, _messageFormatter.Deserialize(workItem.Message)));
            t.Wait();
            return t.Result;            
        }

        async Task DispatchCore(WorkItem workItem, object message)
        {
            _logger.Information(workItem.ToString());

            using (var scope = _dependencyResolver.BeginScope())
            {
                try
                {
                    var t = typeof (IHandler<>);

                    var handlerType = t.MakeGenericType(message.GetType());
                    var mi = handlerType.GetMethod("Run");

                    var handler = scope.GetService(handlerType);

                    await (Task)mi.Invoke(handler, new[] { message });

                    Complete(workItem, handler, message);
                }
                catch (Exception e)
                {
                    if (e.IsFatal())
                        throw;

                    _errorHandlingPolicy.RetryOrPoison(workItem);                
                    _logger.Exception(e);                    
                }
            }

            _logger.Information(workItem.ToString());
        }

        public void Complete(WorkItem workItem, dynamic handler, object message)
        {            
            using (var txs = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            using (var workItemRepository = _workItemRepoitoryProvider.Create())
            {

                foreach (var i in (IEnumerable<object>)handler.NewWorkItems)
                {
                    _workItemQueueClient.Enqueue(i, workItem);
                }

                var t = handler.GetType();
                t.GetMethod("OnComplete").Invoke(handler, new[] { message });

                workItem.Complete();
                workItemRepository.Update(workItem);

                txs.Complete();
            }
        }

        public void Dispose()
        {
            _workItemQueueClient.Dispose();
            _errorHandlingPolicy.Dispose();
        }
    }
}