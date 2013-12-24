using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace BackgroundWorkers
{
    public sealed class AsyncApmAdapter : INotifyCompletion
    {
        IAsyncResult _asyncResult;
        Action _continuation;
        static readonly Action _callback = () => { };

        public AsyncApmAdapter GetAwaiter()
        {
            return this;
        }

        public void OnCompleted(Action continuation)
        {
            if (_continuation == _callback || Interlocked.CompareExchange(ref _continuation, continuation, null) == _callback)
            {
                Task.Run(continuation);
            } 
        }

        public bool IsCompleted
        {
            get
            {
                var iar = _asyncResult;
                return iar != null && (iar.CompletedSynchronously || _continuation == _callback);
            }
        }

        public IAsyncResult GetResult()
        {
            var result = _asyncResult;
            _asyncResult = null;
            _continuation = null;
            return result; 
        }

        public static readonly AsyncCallback Callback = asyncResult =>
        {
            var adapter = (AsyncApmAdapter) asyncResult.AsyncState;
            adapter._asyncResult = asyncResult;

            if (asyncResult.CompletedSynchronously) return;

            var continuation = adapter._continuation ??
                               Interlocked.CompareExchange(ref adapter._continuation, _callback, null);

            if (continuation != null) continuation();

        };

    }

    public static class AsyncResultExtensions
    {
        public static AsyncApmAdapter GetAwaiter(this IAsyncResult iar)
        {
            return (AsyncApmAdapter)iar.AsyncState;
        }
    }
}