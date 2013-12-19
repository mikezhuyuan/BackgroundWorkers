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

            using (var scope = new TransactionScope())
            {
                Configuration.Current.CreateClient().Enqueue(new UrlMessage { Url = "http://www.clubpenguin.com/" });
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
            return builder.Build();
        }
    }


    public class UrlMessage
    {
        public string Url { get; set; }
    }

}
