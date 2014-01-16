using System;
using System.Collections.Generic;
using System.Linq;

namespace BackgroundWorkers
{
    public interface IWorkItemLog
    {
        void WriteException(WorkItem workItem, Exception exception);
    }

    public class WorkItemLog : IWorkItemLog
    {
        readonly IMessageFormatter _messageFormatter;

        public WorkItemLog(IMessageFormatter messageFormatter)
        {
            if (messageFormatter == null) throw new ArgumentNullException("messageFormatter");
            _messageFormatter = messageFormatter;
        }

        public void WriteException(WorkItem workItem, Exception exception)
        {
            var log = workItem.Log ?? "[]";

            var items = ((IEnumerable<object>) _messageFormatter.Deserialize(log, false)).ToList();

            items.Add(new
            {
                Type = "Error",
                ExceptionType = exception.GetType().Name,
                ExceptionDetails = exception.ToString(),
                DispatchCycle = workItem.DispatchCount
            });

            workItem.Log = _messageFormatter.Serialize(items, false);

        }
    }
}