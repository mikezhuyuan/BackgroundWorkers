using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using BackgroundWorkers;

namespace WebCrawler
{
    public class DummyHandler : Handler<DummyMessage>
    {
        public override async Task Run(DummyMessage message)
        {
            Console.WriteLine("Dummy handler is invoked.");
            await Task.Delay(TimeSpan.FromSeconds(10));
            throw new Exception("Doh!");
        }

        public override void OnComplete(DummyMessage message)
        {
            Console.WriteLine("Dummy handler completed.");
        }
    }

    public class DummyMessage
    {
        public string Content { get; set; }
    }
}