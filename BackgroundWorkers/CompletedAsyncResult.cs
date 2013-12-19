using System;
using System.Threading;

namespace BackgroundWorkers
{
    public class CompletedAsyncResult : IAsyncResult
    {
        public CompletedAsyncResult(object state)
        {
            AsyncState = state;
        }

        public bool IsCompleted
        {
            get { return true; }
        }

        public WaitHandle AsyncWaitHandle { get; private set; }
        public object AsyncState { get; private set; }

        public bool CompletedSynchronously
        {
            get { return false; }
        }
    }
}