using System;

namespace BackgroundWorkers.Persistence.Sql
{
    public class SqlWorkItemRepositoryProvider : IWorkItemRepositoryProvider
    {
        readonly string _connectionStringName;

        public SqlWorkItemRepositoryProvider(string connectionStringName)
        {
            if (string.IsNullOrWhiteSpace(connectionStringName)) throw new ArgumentException("A valid connectionStringName is required.");
            _connectionStringName = connectionStringName;
        }

        public IWorkItemRepository Create()
        {
            return new SqlWorkItemRepository(_connectionStringName);
        }
    }
}