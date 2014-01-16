using System;
using Xunit;

namespace BackgroundWorkers.Tests
{
    public class WorkItemFacts
    {
        readonly WorkItem _workItem = new WorkItem("t", "m", "q", Fixture.Now);

        [Fact]
        public void StartsInReadyState()
        {
            Assert.Equal(WorkItemStatus.Ready, _workItem.Status);
            Assert.Equal(WorkItemStatus.Ready, new WorkItem("t", "m", "q", Fixture.Now, _workItem.Id).Status);
        }

        [Fact]
        public void CanMoveToRunningFromReady()
        {
            _workItem.Running();
            Assert.Equal(WorkItemStatus.Running, _workItem.Status);
        }

        [Fact]
        public void CannotMoveToCompletedFromReady()
        {
            Assert.Throws<InvalidOperationException>(() => _workItem.Complete());
        }

        [Fact]
        public void CannotMoveToFailedFromReady()
        {
            Assert.Throws<InvalidOperationException>(() => _workItem.Fail(Fixture.InSeconds(10)));
        }

        [Fact]
        public void CannotMoveToPoisonedFromReady()
        {
            Assert.Throws<InvalidOperationException>(() => _workItem.Poison());
        }

        [Fact]
        public void CanMoveToCompletedFromRunning()
        {
            _workItem.Running();
            _workItem.Complete();
            Assert.Equal(WorkItemStatus.Completed, _workItem.Status);
        }

        [Fact]
        public void CanMoveToFailedFromRunning()
        {
            _workItem.Running();
            _workItem.Fail(Fixture.InSeconds(10));
            Assert.Equal(WorkItemStatus.Failed, _workItem.Status);
        }

        [Fact]
        public void CanMoveToPoisonedFromRunnning()
        {
            _workItem.Running();
            _workItem.Poison();
            Assert.Equal(WorkItemStatus.Poisoned, _workItem.Status);
        }
    }
}