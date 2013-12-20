using System;
using System.Transactions;
using BackgroundWorkers.Persistence;

namespace BackgroundWorkers
{
    public interface IErrorHandlingPolicy
    {
        bool Poison(WorkItem workItem);
        void RetryOrPoison(WorkItem workItem);
    }

    public class ErrorHandlingPolicy : IErrorHandlingPolicy
    {
        readonly ISendMessage<Guid> _poisonedQueue;
        readonly IWorkItemRepositoryProvider _workItemRepositoryProvider;
        readonly Func<DateTime> _now;
        readonly ILogger _logger;
        readonly int _retryCount;

        public ErrorHandlingPolicy(ISendMessage<Guid> poisonedQueue,
            IWorkItemRepositoryProvider workItemRepositoryProvider,
            Func<DateTime> now,
            ILogger logger,
            int retryCount)
        {
            if (poisonedQueue == null) throw new ArgumentNullException("poisonedQueue");
            if (workItemRepositoryProvider == null) throw new ArgumentNullException("workItemRepositoryProvider");
            if (now == null) throw new ArgumentNullException("now");
            if (logger == null) throw new ArgumentNullException("logger");

            _poisonedQueue = poisonedQueue;
            _workItemRepositoryProvider = workItemRepositoryProvider;
            _now = now;
            _logger = logger;
            _retryCount = retryCount;
        }

        public bool Poison(WorkItem workItem)
        {
            if (workItem.DispatchCount > _retryCount)
                return false;

            PoisonCore(workItem);
            return true;
        }

        public void RetryOrPoison(WorkItem workItem)
        {
            try
            {
                if (workItem.DispatchCount < _retryCount)
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
            workItem.Fail(_now() + TimeSpan.FromSeconds(30));
            _workItemRepositoryProvider.Create().Update(workItem);
        }
    }
}