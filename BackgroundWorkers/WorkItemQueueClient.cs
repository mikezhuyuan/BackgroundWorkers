using System;
using System.ServiceModel;

namespace BackgroundWorkers
{
    public interface IWorkItemQueueClient
    {
        void Enqueue(object item);
    }

    public interface IInternalWorkItemQueueClient
    {
        void Enqueue(object item, WorkItem parent);
    }

    public class WorkItemQueueClient : IWorkItemQueueClient, IInternalWorkItemQueueClient
    {
        readonly ISendMessage<NewWorkItem> _queue;
        readonly IMessageFormatter _messageFormatter;
        readonly Func<DateTime> _now;

        public WorkItemQueueClient(ISendMessage<NewWorkItem> queue, IMessageFormatter messageFormatter, Func<DateTime> now)
        {
            if (queue == null) throw new ArgumentNullException("queue");
            if (messageFormatter == null) throw new ArgumentNullException("messageFormatter");
            if (now == null) throw new ArgumentNullException("now");

            _queue = queue;
            _messageFormatter = messageFormatter;
            _now = now;
        }

        public void Enqueue(object item)
        {
            if (item == null) throw new ArgumentNullException("item");
            Enqueue(item, null);
        }

        public void Enqueue(object item, WorkItem parent)
        {
            if (item == null) throw new ArgumentNullException("item");


            _queue.Send(new NewWorkItem
            {
                Body = _messageFormatter.Serialize(item),
                CreatedOn = _now(),
                ParentId = parent == null? (Guid?)null : parent.Id
            });

        }
    }
}