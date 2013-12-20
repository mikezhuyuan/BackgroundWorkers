using System;
using System.Threading.Tasks;
using BackgroundWorkers;

namespace WebCrawler
{
    public class DummyHandler : Handler<DummyMessage>
    {
        public override Task Run(DummyMessage message)
        {
            throw new Exception("Doh1");
        }
    }

    public class DummyMessage
    {
        public string Content { get; set; }
    }
}