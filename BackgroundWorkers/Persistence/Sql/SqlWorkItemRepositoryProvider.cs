using System;

namespace BackgroundWorkers.Persistence.Sql
{
    public class SqlWorkItemRepositoryProvider : IWorkItemRepositoryProvider
    {
        readonly string _connectionStringName;
        readonly Func<DateTime> _now;

        public SqlWorkItemRepositoryProvider(string connectionStringName, Func<DateTime> now)
        {
            if (now == null) throw new ArgumentNullException("now");
            if (string.IsNullOrWhiteSpace(connectionStringName)) throw new ArgumentException("A valid connectionStringName is required.");
            _connectionStringName = connectionStringName;
            _now = now;
        }

        public IWorkItemRepository Create()
        {
            return new SqlWorkItemRepository(_connectionStringName, _now);
        }
    }
}