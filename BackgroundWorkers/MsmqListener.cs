using System;
using System.Messaging;
using System.Threading.Tasks;
using System.Transactions;

namespace BackgroundWorkers
{
    public class MsmqListener<T> : IListenToQueue
    {
        readonly MessageQueue _queue;
        readonly Func<IHandleRawMessage<T>> _func;
        readonly ILogger _logger;
        readonly int _maxWorkers;
        readonly object _sync = new object();
        readonly TaskCompletionSource<object> _taskCompletionSource = new TaskCompletionSource<object>();

        int _activeHandlers;
        bool _isPumping;

        public MsmqListener(MessageQueue queue, Func<IHandleRawMessage<T>> func, ILogger logger, int maxWorkers = 0)
        {
            if (queue == null) throw new ArgumentNullException("queue");
            if (func == null) throw new ArgumentNullException("func");
            if (logger == null) throw new ArgumentNullException("logger");

            _queue = queue;

            _queue.Formatter = new XmlMessageFormatter(new[] {typeof (T)});

            _func = func;
            _logger = logger;
            _maxWorkers = maxWorkers;
        }

        public void Start()
        {
            Pump();
        }

        public void Pump()
        {
            try
            {
                while (true)
                {
                    lock (_sync)
                    {
                        _isPumping = true;
                    }

                    var ar = _queue.BeginPeek(MessageQueue.InfiniteTimeout, null, OnPeek);

                    if (!ar.CompletedSynchronously)
                        break;

                    if (!ShouldPump())
                        break;
                }
            }
            catch (Exception exception)
            {
                if (exception.IsFatal())
                    throw;

                _logger.Exception(exception);
                throw;
            }            
        }

        void OnPeek(IAsyncResult ar)
        {
            IncrementActiveHandlersCountAndStopPump();

            var shouldContinue = true;

            try
            {
                _queue.EndPeek(ar);

                Task handlerTask;
                var handler = _func();
                    
                using (var scope = new TransactionScope())
                {
                    var message = _queue.Receive(MessageQueueTransactionType.Automatic);

                    handlerTask = handler.Run((T) message.Body);

                    scope.Complete();
                }

                if (handlerTask == null) 
                    return;
                
                shouldContinue = false;
                handlerTask.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        if (t.Exception.IsFatal())
                            throw t.Exception;

                        _logger.Exception(t.Exception);
                    }

                    handler.Dispose();

                    if (ShouldPump())
                        Pump();

                }, TaskContinuationOptions.ExecuteSynchronously);
            }
            catch (Exception exception)
            {
                if (exception.IsFatal())
                {
                    shouldContinue = false;
                    throw;
                }

                _logger.Exception(exception);
            }
            finally
            {
                if (!ar.CompletedSynchronously && shouldContinue && ShouldPump())
                    Pump();
            }
        }

        void IncrementActiveHandlersCountAndStopPump()
        {
            lock (_sync)
            {
                _isPumping = false;
                _activeHandlers++;
            }
        }

        bool ShouldPump()
        {
            var shouldPump = false;
            lock (_sync)
            {
                if (!_isPumping && (_maxWorkers == 0 || _activeHandlers < _maxWorkers))
                {
                    _activeHandlers--;
                    shouldPump = _isPumping = true;                    
                }
            }

            return shouldPump;
        }
    }
}