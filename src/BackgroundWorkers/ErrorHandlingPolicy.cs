using System;
using System.Transactions;
using BackgroundWorkers.Persistence;

namespace BackgroundWorkers
{
    public interface IErrorHandlingPolicy : IDisposable
    {
        bool Poison(WorkItem workItem);
        void RetryOrPoison(WorkItem workItem, Exception exception);
    }

    public class ErrorHandlingPolicy : IErrorHandlingPolicy
    {
        readonly ISendMessage<Guid> _poisonedQueue;
        readonly IWorkItemRepositoryProvider _workItemRepositoryProvider;
        readonly IWorkItemLog _workItemLog;
        readonly Func<DateTime> _now;
        readonly ILogger _logger;
        readonly int _retryCount;
        readonly TimeSpan _retryDelay;

        public ErrorHandlingPolicy(ISendMessage<Guid> poisonedQueue, 
            IWorkItemRepositoryProvider workItemRepositoryProvider, 
            IWorkItemLog workItemLog,
            Func<DateTime> now, 
            ILogger logger, 
            int retryCount, 
            TimeSpan retryDelay)
        {
            if (poisonedQueue == null) throw new ArgumentNullException("poisonedQueue");
            if (workItemRepositoryProvider == null) throw new ArgumentNullException("workItemRepositoryProvider");
            if (workItemLog == null) throw new ArgumentNullException("workItemLog");
            if (now == null) throw new ArgumentNullException("now");
            if (logger == null) throw new ArgumentNullException("logger");

            _poisonedQueue = poisonedQueue;
            _workItemRepositoryProvider = workItemRepositoryProvider;
            _workItemLog = workItemLog;
            _now = now;
            _logger = logger;
            _retryCount = retryCount;
            _retryDelay = retryDelay;
        }

        public bool Poison(WorkItem workItem)
        {
            if (CanRetry(workItem))
                return false;

            PoisonCore(workItem);
            return true;
        }

        public void RetryOrPoison(WorkItem workItem, Exception exception)
        {
            if (workItem == null) throw new ArgumentNullException("workItem");
            if (exception == null) throw new ArgumentNullException("exception");

            try
            {
                _workItemLog.WriteException(workItem, exception);

                if (CanRetry(workItem))
                {
                    Retry(workItem);
                }
                else
                {
                    PoisonCore(workItem);
                }
            }
            catch (Exception e)
            {
                if (e.IsFatal())
                    throw;

                _logger.Exception(e);                
            }
        }

        bool CanRetry(WorkItem workItem)
        {
            return workItem.DispatchCount <= _retryCount;
        }

        void PoisonCore(WorkItem workItem)
        {
            using (var scope = new TransactionScope())
            using (var repository = _workItemRepositoryProvider.Create())
            {
                workItem.Poison();
                repository.Update(workItem);
                _poisonedQueue.Send(workItem.Id);
                scope.Complete();
            }
        }

        void Retry(WorkItem workItem)
        {
            workItem.Fail(_now() + _retryDelay);
            _workItemRepositoryProvider.Create().Update(workItem);
        }

        public void Dispose()
        {
            _poisonedQueue.Dispose();
        }
    }
}