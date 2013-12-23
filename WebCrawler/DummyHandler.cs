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
            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }

    public class DummyMessage
    {
        public string Content { get; set; }
    }
}