using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using BackgroundWorkers.Persistence;

namespace BackgroundWorkers
{
    public interface IIncompleteWork
    {
        void Requeue();
    }

    public class IncompleteWork  : IIncompleteWork
    {
        readonly IWorkItemRepositoryProvider _workItemRepositoryProvider;
        Dictionary<string, ISendMessage<Guid>> _clients;

        public IncompleteWork(IWorkItemRepositoryProvider workItemRepositoryProvider, IEnumerable<ISendMessage<Guid>> clients)
        {
            if (workItemRepositoryProvider == null) throw new ArgumentNullException("workItemRepositoryProvider");
            if (clients == null) throw new ArgumentNullException("clients");

            _workItemRepositoryProvider = workItemRepositoryProvider;
            _clients = clients.ToDictionary(c => c.Queue, c => c);
        }

        public void Requeue()
        {
            using(var scope = new TransactionScope())
            using (var repository = _workItemRepositoryProvider.Create())
            {
                foreach (var wi in repository.RunningItems())
                {
                    wi.Ready();
                    repository.Update(wi);
                    _clients[wi.Queue].Send(wi.Id);
                }

                scope.Complete();
            }
        }
    }
}