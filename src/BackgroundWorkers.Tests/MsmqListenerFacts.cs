using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;
using Xunit.Extensions;

namespace BackgroundWorkers.Tests
{
    public class MsmqListenerFacts
    {
        public MsmqListenerFacts()
        {
            TestQueue.Purge();
        }

        [Theory]
        [InlineData("a")]
        public void ReceivesTheFirstMessageInQueue(string body)
        {
            TestQueue.Send(new NewWorkItem { Body  = body });

            var mre = new ManualResetEvent(false);

            var h = Substitute.For<IPrepareWorkItems<NewWorkItem>>();
            h.When(i => i.Prepare(Arg.Is<NewWorkItem>(a => a.Body == body))).Do(c => mre.Set());
            
            var l = new MsmqListener<NewWorkItem>("TestQueue", TestQueue.Queue, () => h, Substitute.For<ILogger>());
            l.Start();

            Assert.True(mre.WaitOne(TimeSpan.FromMinutes(1)));

        }
    }
}