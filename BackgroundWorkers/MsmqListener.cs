using System;
using System.Collections.Generic;
using System.Messaging;
using System.Threading.Tasks;
using System.Transactions;

namespace BackgroundWorkers
{
    public class MsmqListener<T> : IListenToQueue
    {
        readonly string _name;
        readonly MessageQueue _queue;
        readonly Func<IPrepareWorkItems<T>> _func;
        readonly ILogger _logger;
        readonly int _maxWorkers;
        readonly object _sync = new object();
        readonly TaskCompletionSource<object> _taskCompletionSource = new TaskCompletionSource<object>();

        int _activeHandlers;
        bool _isPumping;

        public MsmqListener(string name, MessageQueue queue, Func<IPrepareWorkItems<T>> func, ILogger logger, int maxWorkers = 0)
        {
            if(string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A valid name is required.");
            if (queue == null) throw new ArgumentNullException("queue");
            if (func == null) throw new ArgumentNullException("func");
            if (logger == null) throw new ArgumentNullException("logger");

            _name = name;
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

                    if(!TryPeek())
                        _queue.EndPeek(await _queue.BeginPeek(MessageQueue.InfiniteTimeout, adp, AsyncApmAdapter.Callback));

                    var rawHandler = _func();

                    IEnumerable<WorkItem> preparedWorkItems;

                    using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                    {
                        var message = _queue.Receive(MessageQueueTransactionType.Automatic);

                        preparedWorkItems =  rawHandler.Prepare((T)message.Body);

                        scope.Complete();
                    }

                    var process = rawHandler as IProcessWorkItems;

                    if (process != null)
                        Dispatch(process, preparedWorkItems);
                    else
                        Dispose(rawHandler);

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

        bool TryPeek()
        {
            try
            {
                _queue.Peek(TimeSpan.Zero);
                return true;
            }
            catch (MessageQueueException e)
            {
                if (e.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                    return false;

                throw;
            }
        }

        async void Dispatch(IProcessWorkItems handler, IEnumerable<WorkItem> workItems)
        {
            var exceptionHandled = true;
            try
            {
                var t = handler.Process(workItems);
                await t;
                Dispose(handler);
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

        static void Dispose(object thing)
        {
            var disposable = thing as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
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