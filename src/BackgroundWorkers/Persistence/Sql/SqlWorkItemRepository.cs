using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Transactions;
using Dapper;

namespace BackgroundWorkers.Persistence.Sql
{
    public class SqlWorkItemRepository : IWorkItemRepository
    {
        readonly Func<DateTime> _now;
        readonly SqlConnection _connection;

        const string Columns = "Id, Type, Message, Queue, Status, Version, CreatedOn, ParentId, RetryOn, DispatchCount, Version as NewVersion, Log";

        public SqlWorkItemRepository(string connectionStringName, Func<DateTime> now)
        {
            if (now == null) throw new ArgumentNullException("now");
            _now = now;
            _connection = new SqlConnection(ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString);
            _connection.Open();

            if (Transaction.Current != null)
            {
                _connection.EnlistTransaction(Transaction.Current);
            }
        }

        public WorkItem Find(Guid workItemId)
        {
            return _connection.Query<WorkItem>("select  " + Columns + 
                " from workitems where id = @Id", new {Id = workItemId})
                .Single();
        }

        public IEnumerable<WorkItem> FindAllByParentId(Guid parentWorkItemId)
        {
            return _connection.Query<WorkItem>("select  " + Columns + 
                " from workitems where ParentId = @ParentId", new {ParentId = parentWorkItemId});
        }

        public void Update(WorkItem workItem)
        {
            var count = _connection.Execute("update workitems set " + 
                "status=@Status, DispatchCount=@DispatchCount, RetryOn=@RetryOn, Version=@NewVersion, Log=@Log " + 
                "where id=@Id and version = @Version",
                new { workItem.Id, workItem.Status, workItem.DispatchCount, workItem.RetryOn, workItem.Version, workItem.NewVersion, workItem.Log });

            if (count == 0)
                throw new InvalidOperationException(string.Format("Could not update work item: {0}", workItem));
        }

        public void Add(WorkItem workItem)
        {
            _connection.Execute(
                "insert into workitems ([Id], [Type], [Message], [Queue], [Status], [Version], [CreatedOn], [DispatchCount], [ParentId], [Log]) " + 
                "values (@Id, @Type, @Message, @Queue, @Status, @Version, @CreatedOn, @DispatchCount, @ParentId, @Log);" +
                "select cast(scope_identity() as int)",
                new
                {
                    workItem.Id,
                    workItem.Type,
                    workItem.Message,
                    workItem.Queue,
                    workItem.Status,
                    workItem.Version,
                    workItem.DispatchCount,
                    workItem.CreatedOn,
                    workItem.ParentId,
                    workItem.Log
                });
        }

        public IEnumerable<WorkItem> ReadyToRetry(DateTime now)
        {
            return
                _connection.Query<WorkItem>(
                    "select " + Columns +
                    " from workitems where RetryOn <= @now", new {now});
        }

        public IEnumerable<WorkItem> IncompleteItems()
        {
            return _connection.Query<WorkItem>("select " + Columns + " from workitems where status = @Running or (status = @Failed and retryOn <= @Now)",
                new { WorkItemStatus.Running, WorkItemStatus.Failed, Now = _now()});
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}