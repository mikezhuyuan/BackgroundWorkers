using System;
using System.Threading;
using System.Threading.Tasks;
using BackgroundWorkers;

namespace WebCrawler
{
    public class DummyHandler : Handler<DummyMessage>
    {
        public override Task Run(DummyMessage message)
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(5000);
            });
        }
    }

    public class DummyMessage
    {
        public string Content { get; set; }
    }
}