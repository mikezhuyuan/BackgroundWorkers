using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;

namespace BackgroundWorkers.Demo
{
    static class WorkItemReporter
    {
        public static void Start()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    Thread.Sleep(5000);
                    using (var connection = ConnectionProvider.Connect())
                    {
                        var dict = new Dictionary<string, int>();
                        var rows = connection.Query(@"select status, count = count(1) from workitems group by status");
                        foreach (var row in rows)
                        {
                            dict[((WorkItemStatus)row.status).ToString()] = row.count;
                        }

                        AppHub.WorkItems(dict);
                    }
                }
            });
        }
    }
}
