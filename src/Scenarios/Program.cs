using System;
using System.Collections.Generic;
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
                .UseMergeableWorkItemQueueName("Scenarios.MergeableWorkItems")
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
                WorkersConfiguration.Current.CreateClient().Enqueue(new ItemsMessage
                {
                    Items = new[] {1,2,3}
                });
                scope.Complete();
            }
            Console.ReadLine();
        }

        static ILifetimeScope BuildContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ItemsHandler>().As<IHandler<ItemsMessage>>();
            builder.RegisterType<MergedItemsHandler>().As<IHandler<MergedMessages<ItemMessage>>>();
            return builder.Build();
        }
    }

    public class ItemsMessage
    {
        public int[] Items { get; set; }
    }

    public class ItemMessage
    {
        public int Item { get; set; }
    }

    public class MergedItemsMessage : MergedMessages<ItemMessage>
    {
        
    }

    public class ItemsHandler : ForkHandler<ItemsMessage, ItemMessage>
    {
        public override Task Run(ItemsMessage message)
        {
            foreach (var itm in message.Items)
            {
                ForkNewWork(new ItemMessage {Item = itm});
            }

            return Task.FromResult((object) null);
        }
    }

    public class MergedItemsHandler : Handler<MergedMessages<ItemMessage>>
    {
        public override Task Run(MergedMessages<ItemMessage> merged)
        {
            foreach (var itm in (IEnumerable<object>)merged.Messages)
            {
                var m = (ItemMessage) itm;
                Console.WriteLine(m.Item);
            }

            return Task.FromResult((object)null);
        }
    }
}
