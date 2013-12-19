using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using Dapper;

namespace BackgroundWorkers.Persistence.Sql
{
    public class SqlWorkItemRepository : IWorkItemRepository
    {
        readonly SqlConnection _connection;

        const string Columns = "Id, Type, Message, Queue, Status, Version, CreatedOn, ParentId, RetryOn, DispatchCount, Version as NewVersion";

        public SqlWorkItemRepository(string connectionStringName)
        {
            _connection = new SqlConnection(ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString);
            _connection.Open();
        }

        public WorkItem Find(Guid workItemId)
        {
            return _connection.Query<WorkItem>("select  " + Columns + 
                " from workitems where id = @Id", new {Id = workItemId})
                .Single();
        }

        public void Update(WorkItem workItem)
        {
            var count = _connection.Execute("update workitems set " + 
                "status=@Status, DispatchCount=@DispatchCount, RetryOn=@RetryOn, Version=@NewVersion " + 
                "where id=@Id and version = @Version",
                new { workItem.Id, workItem.Status, workItem.DispatchCount, workItem.RetryOn, workItem.Version, workItem.NewVersion });

            if (count == 0)
                throw new InvalidOperationException(string.Format("Could not update work item: {0}", workItem));
        }

        public void Add(WorkItem workItem)
        {
            _connection.Execute(
                "insert into workitems ([Id], [Type], [Message], [Queue], [Status], [Version], [CreatedOn], [DispatchCount], [ParentId]) " + 
                "values (@Id, @Type, @Message, @Queue, @Status, @Version, @CreatedOn, @DispatchCount, @ParentId);" +
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
                    workItem.ParentId
                });
        }

        public IEnumerable<WorkItem> ReadyToRetry(DateTime now)
        {
            return
                _connection.Query<WorkItem>(
                    "select " + Columns +
                    " from workitems where RetryOn <= @now", new {now});
        }

        public IEnumerable<WorkItem> RunningItems()
        {
            return _connection.Query<WorkItem>("select " + Columns + " from workitems where status = @Status",
                new {Status = WorkItemStatus.Running});
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}