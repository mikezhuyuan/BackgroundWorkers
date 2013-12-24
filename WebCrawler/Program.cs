using System;
using System.Configuration;
using System.Transactions;
using Autofac;
using BackgroundWorkers;
using BackgroundWorkers.Integration.Autofac;
using BackgroundWorkers.Persistence.Sql;

namespace WebCrawler
{
    class Program
    {
        static void Main()
        {
            WorkItemsTable.Create(ConfigurationManager.ConnectionStrings["WebCrawler"].ConnectionString);

            WorkersConfiguration.Current
                    .UseDependencyResolver(new AutofacDependencyResolver(BuildContainer()))
                    .UseWorkItemRepositoryProvider(new SqlWorkItemRepositoryProvider("WebCrawler"))
                    .WithQueue("WebCrawler", 32)
                    .CreateHost()
                    .Start();

            Console.WriteLine("Enter the URL to start");
            var url = Console.ReadLine();

            using (var scope = new TransactionScope())
            using(var client = WorkersConfiguration.Current.CreateClient())
            {
                //client.Enqueue(new UrlMessage { Url = url });
                client.Enqueue(new DummyMessage());
                //client.Enqueue(new DummyMessage());
                //client.Enqueue(new DummyMessage());
                scope.Complete();
            }

            Console.WriteLine("Crawling...");            
            Console.ReadLine();
            Console.WriteLine("Crawler stopped.");
        }

        static ILifetimeScope BuildContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<WebCrawler>().As<IHandler<UrlMessage>>();
            builder.RegisterType<DummyHandler>().As<IHandler<DummyMessage>>();
            return builder.Build();
        }
    }

}
