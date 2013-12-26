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
            if(AcquirePump())
                Pump();

            return _taskCompletionSource.Task;
        }

        public async Task Pump()
        {
            while (true)
            {
                try
                {                    
                    var adp = new AsyncApmAdapter();
                    _queue.EndPeek(await _queue.BeginPeek(MessageQueue.InfiniteTimeout, adp, AsyncApmAdapter.Callback));

                    var rawHandler = _func();

                    Message message;
                    
                    using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                    {
                        message = _queue.Receive(MessageQueueTransactionType.Automatic);

                        rawHandler.OnDequeue((T)message.Body);

                        scope.Complete();
                    }

                    DispatchMessageToRawHandler(rawHandler, message);

                    lock (_sync)
                    {
                        _isPumping = false;
                    
                        if (!AcquirePump())
                            break;                    
                    }                   
                }
                catch (Exception exception)
                {
                    if(!HandlePumpException(exception))
                        break;
                }    
            }                        
        }

        async void DispatchMessageToRawHandler(IHandleRawMessage<T> handler, Message message)
        {
            var exceptionHandled = true;
            try
            {
                await handler.Run((T)message.Body);
                handler.Dispose();
            }
            catch (Exception exception)
            {
                exceptionHandled = HandlePumpException(exception);
            }
            finally
            {
                if (AcquirePump(true) && exceptionHandled)
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
            
            _taskCompletionSource.TrySetException(exception);
            return false;
        }

        bool AcquirePump(bool release = false)
        {
            lock (_sync)
            {
                if (release)
                {
                    _activeHandlers--;
                }

                if (_isPumping) return false;

                if (_maxWorkers == 0 || _activeHandlers < _maxWorkers)
                {
                    _activeHandlers++;
                    return _isPumping = true;
                }

                return false;
            }
        }
    }
}