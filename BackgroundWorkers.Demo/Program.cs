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
#if DEBUG
                        c.RetryCount = 2; 
                        c.MaxWorkers = 100;
#else
                        c.RetryCount = 2;
                        c.MaxWorkers = 100;
#endif
                        c.ListenTo<ScrapePageMessage>();
                    })
                    .WithQueue("WebCrawler.Screenshot", c =>
                    {
#if DEBUG
                        c.RetryCount = 2;
                        c.MaxWorkers = 4;
#else
                        c.RetryCount = 2;
                        c.MaxWorkers = 32;    
#endif
                        c.ListenTo<CapturePageMessage>();
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
