using System;
using Autofac;
using BackgroundWorkers.Demo.Handlers;
using BackgroundWorkers.Integration.Autofac;
using BackgroundWorkers.Persistence.Sql;
using Microsoft.Owin.Hosting;

namespace BackgroundWorkers.Demo
{
    class Program
    {
        static void Main()
        {
            Start();

            Console.WriteLine("Running...");
            Console.WriteLine("Please visit http://localhost/webcrawler/index.html in Chrome.");
            Console.ReadLine();
        }

        static void Start()
        {
            WebApp.Start<WebAppStartup>("http://*:80/webcrawler");

            WorkItemsTable.Create(ConnectionProvider.ConnectionString);

            WorkersConfiguration.Current
                    .UseDependencyResolver(new AutofacDependencyResolver(BuildContainer()))
                    .UseWorkItemRepositoryProvider(new SqlWorkItemRepositoryProvider("WebCrawler"))
                    .WithQueue("WebCrawler", c =>
                    {
                        c.RetryCount = 2; 
                        c.MaxWorkers = 2;
                        c.ListenTo<ScrapePage>();
                    })
                    .WithQueue("WebCrawler.Screenshot", c =>
                    {
                        c.RetryCount = 2;
                        c.MaxWorkers = 4;
                        c.ListenTo<CapturePage>();
                    })
                    .UseLogger(new Logger())                    
                    .CreateHost()
                    .Start();

            WorkItemReporter.Start();
        }

        static ILifetimeScope BuildContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<CapturePage>().As<IHandler<CapturePageMessage>>();
            builder.RegisterType<ScrapePage>().As<IHandler<ScrapePageMessage>>();
            return builder.Build();
        }
    }
}
