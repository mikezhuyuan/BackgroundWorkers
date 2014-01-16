using System.Messaging;

namespace BackgroundWorkers
{
    public class MsmqQueue<T> : ISendMessage<T>
    {
        readonly MessageQueue _queue;

        public MsmqQueue(MessageQueue queue)
        {
            _queue = queue;
            _queue.Formatter = new XmlMessageFormatter(new[] {typeof (T)});
        }

        public void Send(T message)
        {
            var msg = new Message(message, new XmlMessageFormatter(new[] {typeof (T)}))
            {
               Recoverable = true
            };
            _queue.Send(msg, MessageQueueTransactionType.Automatic);
        }

        public string Queue { get { return _queue.QueueName; } }

        public void Dispose()
        {
            _queue.Dispose();
        }
    }
}