﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using BackgroundWorkers.Persistence;

namespace BackgroundWorkers
{
    public class WorkItemDispatcher : IPrepareWorkItems<Guid>, IProcessWorkItems, IDisposable
    {
        readonly IDependencyResolver _dependencyResolver;
        readonly IWorkItemRepositoryProvider _workItemRepoitoryProvider;
        readonly IInternalWorkItemQueueClient _workItemQueueClient;
        readonly IMessageFormatter _messageFormatter;
        readonly IErrorHandlingPolicy _errorHandlingPolicy;
        readonly ILogger _logger;
        readonly PerformanceCounter _throughput;
        readonly PerformanceCounter _count;

        public WorkItemDispatcher(string name, IDependencyResolver dependencyResolver, IWorkItemRepositoryProvider workItemRepoitoryProvider, IInternalWorkItemQueueClient workItemQueueClient, IMessageFormatter messageFormatter, IErrorHandlingPolicy errorHandlingPolicy, ILogger logger)
        {
            if(string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A valid name is required.");
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

            _throughput = new PerformanceCounter(PerformanceCounterConstants.Category,
                string.Format("{0}/sec", name), false);

            _count = new PerformanceCounter(PerformanceCounterConstants.Category, 
                string.Format("{0} count", name), false);
        }

        public IEnumerable<WorkItem> Prepare(Guid message)
        {
            _logger.Information("WI-{0} - Dispatching", message);

            using (var repository = _workItemRepoitoryProvider.Create())
            {
                var workItem = repository.Find(message);

                if (workItem == null)
                {
                    _logger.Warning("WI-{0} - Could not be found in the repository.", message);
                    yield break;
                }

                if (!workItem.Running())
                {
                    _logger.Information("WI did not run {0}", workItem);
                    yield break;
                }

                repository.Update(workItem);

                yield return workItem;
            }

        }

        public Task Process(IEnumerable<WorkItem> workItems)
        {
            if (workItems == null) throw new ArgumentNullException("workItems");

            // Schedule the handler as a new task because we don't want the code in handler to
            // block the dispatcher.
            var workItem = workItems.Single();
            return Task.Run(() => DispatchCore(workItem, workItem.Message));
        }

        async Task DispatchCore(WorkItem workItem, string rawMessage)
        {
            _logger.Information(workItem.ToString());
            _count.Increment();

            var message = _messageFormatter.Deserialize(rawMessage);
            using (var scope = _dependencyResolver.BeginScope())
            {
                try
                {
                    if (message.GetType() == typeof (MergeableMessage))
                    {
                        var handler = new MergeableMessageHandler(_workItemRepoitoryProvider, _messageFormatter);
                        await handler.Run((MergeableMessage)message, workItem);
                        Complete(workItem, handler, message);
                    }
                    else
                    {
                        var t = typeof (IHandler<>);

                        var handlerType = t.MakeGenericType(message.GetType());
                        var mi = handlerType.GetMethod("Run");

                        var handler = scope.GetService(handlerType);

                        await (Task) mi.Invoke(handler, new[] {message});

                        Complete(workItem, handler, message);
                    }
                }
                catch (Exception e)
                {
                    if (e.IsFatal())
                        throw;

                    _errorHandlingPolicy.RetryOrPoison(workItem, e);
                    _logger.Exception(e);
                }
                finally
                {
                    _count.Decrement();
                    _throughput.Increment();
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