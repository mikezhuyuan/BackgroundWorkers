using System;
using System.Threading.Tasks;
using System.Transactions;
using Autofac;
using BackgroundWorkers;
using BackgroundWorkers.Integration.Autofac;

namespace WebCrawler
{
    class Program
    {
        static void Main(string[] args)
        {

            Configuration.Current
                .UseDependencyResolver(new AutofacDependencyResolver(BuildContainer()))
                .WithQueue("WebCrawler")
                .CreateHost().Start();

            Console.WriteLine("Enter the URL to start");
            var url = Console.ReadLine();

            using (var scope = new TransactionScope())
            using(var client = Configuration.Current.CreateClient())
            {
                client.Enqueue(new UrlMessage {Url = url });
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
