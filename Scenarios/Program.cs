using System;
using System.Configuration;
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

            var config = WorkersConfiguration.Current
                    .UseDependencyResolver(new AutofacDependencyResolver(BuildContainer()))
                    .UseWorkItemRepositoryProvider(new SqlWorkItemRepositoryProvider("Scenarios", () => DateTime.Now))
                    .UseNewWorkItemsQueueName("Scenarios.NewWorkItems")
                    .UsePoisonedWorkItemsQueueName("Scenarios.PoisonedWorkItems")
                    .WithQueue("Scenarios", c =>
                    {
                        c.RetryCount = 2;
                        c.MaxWorkers = 10;
                        c.ListenToAll();
                    });
            
            config.CreatePerformanceCounters();
            config.CreateHost().Start();

            using (var scope = new TransactionScope())
            {
                WorkersConfiguration.Current.CreateClient().Enqueue(new LongRunningWorkItem());
                WorkersConfiguration.Current.CreateClient().Enqueue(new FailingWorkItem());
                scope.Complete();
            }
            Console.ReadLine();
        }

        static ILifetimeScope BuildContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<LongRunningWorkItemHandler>().As<IHandler<LongRunningWorkItem>>();
            builder.RegisterType<FailingWorkItemHandler>().As<IHandler<FailingWorkItem>>();
            builder.RegisterType<FailingWorkItemFaultHandler>().As<IHandleFault<FailingWorkItem>>();
            return builder.Build();
        }
    }

    public class LongRunningWorkItem
    {
        
    }

    public class FailingWorkItem
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

    public class FailingWorkItemHandler : Handler<FailingWorkItem>
    {
        public override Task Run(FailingWorkItem message)
        {
            throw new Exception("This is not working");
        }
    }

    public class FailingWorkItemFaultHandler : IHandleFault<FailingWorkItem>
    {
        public void Run(FailingWorkItem message, string log)
        {
            Console.WriteLine(log);
        }
    }
}
