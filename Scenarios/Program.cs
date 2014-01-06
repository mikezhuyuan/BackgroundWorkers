using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Autofac;
using BackgroundWorkers;
using BackgroundWorkers.Integration.Autofac;
using BackgroundWorkers.Persistence.Sql;

namespace Scenarios
{
    class Program
    {
        static void Main(string[] args)
        {
            
            WorkItemsTable.Create(ConfigurationManager.ConnectionStrings["Scenarios"].ConnectionString);

            WorkersConfiguration.Current
                    .UseDependencyResolver(new AutofacDependencyResolver(BuildContainer()))
                    .UseWorkItemRepositoryProvider(new SqlWorkItemRepositoryProvider("Scenarios"))
                    .WithQueue("Scenarios", c =>
                    {
                        c.RetryCount = 2;
                        c.MaxWorkers = 10;
                        c.ListenTo<LongRunningWorkItem>();
                    })                    
                    .CreateHost()
                    .Start();

            using (var scope = new TransactionScope())
            {
                WorkersConfiguration.Current.CreateClient().Enqueue(new LongRunningWorkItem());
                scope.Complete();
            }
            Console.ReadLine();
        }

        static ILifetimeScope BuildContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<LongRunningWorkItemHandler>().As<IHandler<LongRunningWorkItem>>();            
            return builder.Build();
        }
    }

    public class LongRunningWorkItem
    {
        
    }

    public class LongRunningWorkItemHandler : Handler<LongRunningWorkItem>
    {
        public override async Task Run(LongRunningWorkItem message)
        {
            await Task.Delay(TimeSpan.FromMinutes(1));
            Console.WriteLine("Long running work is complete");
            NewWorkItems.Add(new LongRunningWorkItem());
        }
    }
}
