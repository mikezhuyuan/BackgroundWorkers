using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BackgroundWorkers.Persistence;

namespace BackgroundWorkers
{
    class MergeableMessageHandler : Handler<MergeableMessage>
    {
        readonly IWorkItemRepositoryProvider _workItemRepoitoryProvider;
        readonly IMessageFormatter _messageFormatter;

        public MergeableMessageHandler(IWorkItemRepositoryProvider workItemRepoitoryProvider, IMessageFormatter messageFormatter)
        {
            if (workItemRepoitoryProvider == null) throw new ArgumentNullException("workItemRepoitoryProvider");
            if (messageFormatter == null) throw new ArgumentNullException("messageFormatter");

            _workItemRepoitoryProvider = workItemRepoitoryProvider;
            _messageFormatter = messageFormatter;
        }

        public override async Task Run(MergeableMessage message)
        {
        }

        public Task Run(MergeableMessage message, WorkItem workItem)
        {
            if (!workItem.ParentId.HasValue) 
                return Task.FromResult((object) null);

            using (var repository = _workItemRepoitoryProvider.Create())
            {
                var workItems = repository.FindAllByParentId(workItem.ParentId.Value);
                if (workItems.Any(i => i.Id != workItem.Id && i.Status != WorkItemStatus.Completed))
                    return Task.FromResult((object) null);

                var messages = new List<object>();
                foreach (var wi in workItems)
                {
                    messages.Add(((MergeableMessage)_messageFormatter.Deserialize(wi.Message)).Body);
                }

                dynamic result = Activator.CreateInstance(typeof(MergedMessages<>).MakeGenericType(message.Body.GetType()));
                result.SetMessages(messages);

                NewWorkItems.Add(result);
            }

            return Task.FromResult((object)null);
        }
    }
}
