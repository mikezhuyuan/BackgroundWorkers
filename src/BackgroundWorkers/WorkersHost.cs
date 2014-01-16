using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackgroundWorkers
{
    public class WorkersHost : IDisposable
    {
        readonly IEnumerable<IListenToQueue> _workItemDispatchers;
        readonly IListenToQueue _poisonedWorkItemDispatcher;
        readonly IListenToQueue _workItemQueue;
        readonly IRetryClock _retryClock;
        readonly IIncompleteWork _incompleteWork;

        public WorkersHost(IEnumerable<IListenToQueue> workItemDispatchers,
            IListenToQueue workItemQueue, 
            IListenToQueue poisonedWorkItemDispatcher, 
            IRetryClock retryClock, 
            IIncompleteWork incompleteWork)
        {
            if (workItemDispatchers == null) throw new ArgumentNullException("workItemDispatchers");
            if (poisonedWorkItemDispatcher == null) throw new ArgumentNullException("poisonedWorkItemDispatcher");
            if (workItemQueue == null) throw new ArgumentNullException("workItemQueue");
            if (incompleteWork == null) throw new ArgumentNullException("incompleteWork");

            _workItemDispatchers = workItemDispatchers;
            _poisonedWorkItemDispatcher = poisonedWorkItemDispatcher;
            _workItemQueue = workItemQueue;
            _retryClock = retryClock;
            _incompleteWork = incompleteWork;
        }

        public async void Start()
        {
            var listeners = _workItemDispatchers.Union(new[] { _workItemQueue, _poisonedWorkItemDispatcher });
            _incompleteWork.Requeue();

            var tasks = listeners.Select(l => Task.Factory.StartNew(() => l.Start()))
                                  .ToArray();
            
            _retryClock.Start();

            Task.WaitAll(tasks);
 
            var firstToFinish = await Task.WhenAny(tasks.Select(t => t.Result));

            await firstToFinish;
        }


        public void Dispose()
        {
        }
    }
}