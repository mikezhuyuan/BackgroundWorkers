using System;
using BackgroundWorkers.Persistence;
using NSubstitute;
using Xunit;

namespace BackgroundWorkers.Tests
{
    public class ErrorHandlingPolicyFacts
    {
        public class PoisonMethod
        {
            [Fact]
            public void DoesNotPoisonWorkItemWhichHasNotReachedMaxRetryCycles()
            {
                var f = new ErrorHandlingPolicyFixture();
                
                var wi = new WorkItem("t", "m", "q", Fixture.Now);
                wi.Running();
                Assert.True(f.Subject.Poison(wi));
            }
        }
    }

    public class ErrorHandlingPolicyFixture : IFixture<ErrorHandlingPolicy>
    {
        public ISendMessage<Guid> PoisonQueue { get; set; }
        
        public IWorkItemRepositoryProvider WorkItemRepositoryProvider { get; set; }

        public Func<DateTime> Now { get; set; }

        public ILogger Logger { get; set; }

        public int RetryCount { get; set; }

        public ErrorHandlingPolicyFixture()
        {
            PoisonQueue = Substitute.For<ISendMessage<Guid>>();
            WorkItemRepositoryProvider = Substitute.For<IWorkItemRepositoryProvider>();
            Now = () => Fixture.Now;
            Logger = Substitute.For<ILogger>();
            
        }

        public ErrorHandlingPolicy Subject { get
        {
            return new ErrorHandlingPolicy(PoisonQueue, WorkItemRepositoryProvider, Now, Logger, RetryCount);
        } }
    }
}