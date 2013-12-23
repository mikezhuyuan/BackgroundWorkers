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

        public Task Start()
        {
            Pump();
            return _taskCompletionSource.Task;
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

                    if (!AcquirePump())
                        break;
                }
            }
            catch (Exception exception)
            {
                if (!exception.IsFatal())
                    _logger.Exception(exception);

                _taskCompletionSource.TrySetException(exception);
            }            
        }

        void OnPeek(IAsyncResult ar)
        {
            IncrementActiveHandlersCountAndStopPump();

            var shouldContinue = true;

            Task handlerTask = null;
            var rawHandler = _func();

            try
            {
                _queue.EndPeek(ar);

                using (var scope = new TransactionScope())
                {
                    var message = _queue.Receive(MessageQueueTransactionType.Automatic);

                    handlerTask = rawHandler.Run((T)message.Body);

                    scope.Complete();
                }

                handlerTask.ContinueWith(a =>
                {
                    if (a.Status == TaskStatus.Faulted && !HandlePumpException(a.Exception))
                    {
                        return;                                                 
                    }

                    rawHandler.Dispose();

                    if (!ar.CompletedSynchronously && AcquirePump())
                        Pump();
                });
            }
            catch (Exception exception)
            {
                shouldContinue = HandlePumpException(exception);
            }
            finally
            {
                if (!ar.CompletedSynchronously && shouldContinue && AcquirePump(handlerTask == null))
                    Pump();
            }
        }

        bool HandlePumpException(Exception exception)
        {
            if (exception.IsFatal())
            {
                _taskCompletionSource.TrySetException(exception);
                return false;
            }

            _logger.Exception(exception);

            if (!(exception is MessageQueueException)) return true;
            
            _taskCompletionSource.SetException(exception);
            return false;
        }

        void IncrementActiveHandlersCountAndStopPump()
        {
            lock (_sync)
            {
                _isPumping = false;
                _activeHandlers++;
            }
        }

        bool AcquirePump(bool canRelease = true)
        {
            lock (_sync)
            {
                if (canRelease)
                {
                    _activeHandlers--;
                }

                if (_isPumping) return false;

                if (_maxWorkers == 0) return true;
              
                if (_activeHandlers < _maxWorkers)
                {
                    return _isPumping = true;
                }

                return false;
            }
        }
    }
}