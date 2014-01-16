using System;
using Xunit;

namespace BackgroundWorkers.Tests
{
    public class WorkItemLogFacts
    {
        [Fact]
        public void WritesExceptionToLog()
        {
            var wi = new WorkItem();

            var f = new WorkItemLogFixture();
            f.Subject.WriteException(wi, new InvalidOperationException());

            Assert.Contains("InvalidOperationException", wi.Log);
        }

        [Fact]
        public void PreservesTheExistingLog()
        {
            var wi = new WorkItem();

            var f = new WorkItemLogFixture();
            f.Subject.WriteException(wi, new InvalidOperationException());
            f.Subject.WriteException(wi, new TimeoutException());

            Assert.Contains("InvalidOperationException", wi.Log);
            Assert.Contains("TimeoutException", wi.Log);
        }
    }

    public class WorkItemLogFixture : IFixture<WorkItemLog>
    {
        public IMessageFormatter MessageFormatter { get; set; }

        public WorkItemLogFixture()
        {
            MessageFormatter = new MessageFormatter();
        }

        public WorkItemLog Subject
        {
            get { return new WorkItemLog(MessageFormatter); }
        }
    }
}