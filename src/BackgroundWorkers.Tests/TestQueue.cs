using System.Messaging;
using System.Transactions;

namespace BackgroundWorkers.Tests
{
    public static class TestQueue
    {
        public const string Path = ".\\private$\\BackgroundWorkersTests";

        static TestQueue()
        {
            if (!MessageQueue.Exists(Path))
                MessageQueue.Create(Path, true);
        }
        public static void Send(object message)
        {
            using(var scope = new TransactionScope())
            using (var q = new MessageQueue(Path))
            {
                q.Formatter = new XmlMessageFormatter(new[] { message.GetType() } );
                q.Send(message, MessageQueueTransactionType.Automatic);
                scope.Complete();
            }
        }

        public static void Purge()
        {
            using (var q = new MessageQueue(Path))
            {
                q.Purge();
            }
        }

        public static MessageQueue Queue
        {
            get
            {
                return new MessageQueue(Path);
            }
        }
    }
}