using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Transactions;
using BackgroundWorkers.Persistence;

namespace BackgroundWorkers
{
    public interface IRetryClock
    {
        void Start();
    }

    public class RetryClock : IRetryClock
    {
        readonly TimeSpan _delay;
        readonly ILogger _logger;
        readonly IWorkItemRepositoryProvider _workItemRepositoryProvider;
        readonly Func<DateTime> _now;
        readonly Dictionary<string, ISendMessage<Guid>> _clients;
        readonly Timer _timer;

        void OnTick(object state)
        {
            try
            {
                IEnumerable<WorkItem> items;
                using (var repository = _workItemRepositoryProvider.Create())
                {
                    items = repository.ReadyToRetry(_now());

                }
                foreach (var wi in items)
                {
                    using(var scope = new TransactionScope())
                    using (var repository = _workItemRepositoryProvider.Create())
                    {
                        wi.Ready();
                        repository.Update(wi);
                        _clients[wi.Queue].Send(wi.Id);
                        scope.Complete();    
                    }                                       
                }
            }
            catch (Exception e)
            {
                if (e.IsFatal())
                    throw;

                _logger.Exception(e);
            }
            finally
            {
                _timer.Change(_delay, Timeout.InfiniteTimeSpan);
            }
        }

        public RetryClock(TimeSpan delay, ILogger logger, IWorkItemRepositoryProvider workItemRepositoryProvider, Func<DateTime> now, IEnumerable<ISendMessage<Guid>> clients)
        {
            if (logger == null) throw new ArgumentNullException("logger");
            if (workItemRepositoryProvider == null) throw new ArgumentNullException("workItemRepositoryProvider");
            if (now == null) throw new ArgumentNullException("now");
            if (clients == null) throw new ArgumentNullException("clients");

            _delay = delay;
            _logger = logger;
            _workItemRepositoryProvider = workItemRepositoryProvider;
            _now = now;
            _clients = clients.ToDictionary(c => c.Queue, c => c);

            _timer = new Timer(OnTick);
        }

        public void Start()
        {
            _timer.Change(_delay, Timeout.InfiniteTimeSpan);
        }
    }
}