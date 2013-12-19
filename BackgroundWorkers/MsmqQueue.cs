using System.Messaging;

namespace BackgroundWorkers
{
    public class MsmqQueue<T> : IMessageQueue<T>
    {
        readonly MessageQueue _queue;

        public MsmqQueue(MessageQueue queue)
        {
            _queue = queue;
            _queue.Formatter = new XmlMessageFormatter(new[] {typeof (T)});
        }

        public void Send(T message)
        {
            _queue.Send(new Message(message, new XmlMessageFormatter(new[] {typeof (T)})), MessageQueueTransactionType.Automatic);
        }

        public string Queue { get { return _queue.QueueName; } }
    }
}