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

            var h = Substitute.For<IHandleRawMessage<NewWorkItem>>();
            h.When(i => i.Run(Arg.Is<NewWorkItem>(a => a.Body == body))).Do(c => mre.Set());
            h.Run(null).ReturnsForAnyArgs(Task.FromResult<Task>(null));

            var l = new MsmqListener<NewWorkItem>(TestQueue.Queue, () => h, Substitute.For<ILogger>());
            l.Start();

            Assert.True(mre.WaitOne(TimeSpan.FromMinutes(1)));

        }
    }
}