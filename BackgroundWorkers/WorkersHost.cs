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
            IListenToQueue poisonedWorkItemDispatcher, 
            IListenToQueue workItemQueue, 
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
            
            _incompleteWork.Requeue();

            var tasks = _workItemDispatchers
                .Select(wd => Task.Factory.StartNew(() => wd.Start()))
                .ToList();
            tasks.Add(Task.Run<Task>(() => _poisonedWorkItemDispatcher.Start()));
            tasks.Add(Task.Run<Task>(() => _workItemQueue.Start()));
            _retryClock.Start();

            Task.WaitAll(tasks.ToArray());
 
            var firstToFinish = await Task.WhenAny(tasks.Select(t => t.Result));

            await firstToFinish;
        }


        public void Dispose()
        {
        }
    }
}