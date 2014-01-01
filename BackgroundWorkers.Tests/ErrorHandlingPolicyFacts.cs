using System;
using BackgroundWorkers.Persistence;
using NSubstitute;
using Xunit;
using Xunit.Extensions;

namespace BackgroundWorkers.Tests
{
    public class ErrorHandlingPolicyFacts
    {
        public class PoisonMethod
        {
            readonly WorkItem _workItem = new WorkItem("t", "m", "q", Fixture.Now);

            [Fact]
            public void DoesNotPoisonWorkItemWhichHasNotReachedMaxRetryCycles()
            {
                var f = new ErrorHandlingPolicyFixture
                {
                    RetryCount = 1
                };

                _workItem.Running();
                Assert.False(f.Subject.Poison(_workItem));
                Assert.Equal(WorkItemStatus.Running, _workItem.Status);
                f.WorkItemRepositoryProvider.DidNotReceiveWithAnyArgs().Create();
            }

            [Fact]
            public void PoisonsWorkItemAfterMaxRetryCycles()
            {
                var f = new ErrorHandlingPolicyFixture
                {
                    RetryCount = 1
                };

                _workItem.Running();
                _workItem.Fail(Fixture.Now);
                _workItem.Ready();
                _workItem.Running();

                Assert.True(f.Subject.Poison(_workItem));
                Assert.Equal(WorkItemStatus.Poisoned, _workItem.Status);
                
                f.WorkItemRepository.Received(1).Update(_workItem);
            } 

        }

        public class RetryOrPoisonMethod
        {
            readonly WorkItem _workItem = new WorkItem("t", "m", "q", Fixture.Now);

            [Fact]
            public void RetriesWorkItemWhichHasNotReachedMaxRetryCycles()
            {
                var f = new ErrorHandlingPolicyFixture
                {
                    RetryCount = 1
                };

                _workItem.Running();

                f.Subject.RetryOrPoison(_workItem);

                Assert.Equal(WorkItemStatus.Failed, _workItem.Status);
                f.WorkItemRepository.Received().Update(_workItem);
                Assert.Equal(f.Now() + f.RetryDelay, _workItem.RetryOn);

            }

            [Fact]
            public void PoisonsWorkItemAfterMaxRetryCycles()
            {
                var f = new ErrorHandlingPolicyFixture();
                
                _workItem.Running();

                f.Subject.RetryOrPoison(_workItem);

                Assert.Equal(WorkItemStatus.Poisoned, _workItem.Status);
                f.WorkItemRepository.Received(1).Update(_workItem);
            }
        }
    }

    public class ErrorHandlingPolicyFixture : IFixture<ErrorHandlingPolicy>
    {
        public ISendMessage<Guid> PoisonQueue { get; set; }
        
        public IWorkItemRepositoryProvider WorkItemRepositoryProvider { get; set; }

        public IWorkItemRepository WorkItemRepository { get; set; }

        public Func<DateTime> Now { get; set; }

        public ILogger Logger { get; set; }

        public int RetryCount { get; set; }

        public TimeSpan RetryDelay { get; set; }

        public ErrorHandlingPolicyFixture()
        {
            PoisonQueue = Substitute.For<ISendMessage<Guid>>();
            
            WorkItemRepositoryProvider = Substitute.For<IWorkItemRepositoryProvider>();
            WorkItemRepository = Substitute.For<IWorkItemRepository>();

            WorkItemRepositoryProvider.Create().Returns(WorkItemRepository);

            Now = () => Fixture.Now;
            Logger = Substitute.For<ILogger>();
            
        }

        public ErrorHandlingPolicy Subject { get
        {
            return new ErrorHandlingPolicy(PoisonQueue, WorkItemRepositoryProvider, Now, Logger, RetryCount, RetryDelay);
        } }
    }
}