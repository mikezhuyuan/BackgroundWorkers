using System;

namespace BackgroundWorkers
{
    public enum WorkItemStatus
    {
        Ready,
        Running,
        Failed,
        Completed,
        Poisoned
    }

    public class WorkItem
    {
        [Obsolete("For persistence only")]
        public WorkItem()
        {
        }

        public WorkItem(string type, string message, string queue, DateTime createdOn, Guid? parentId = null)
        {
            if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("A valid type is required.");
            if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("A valid message is required.");
            if (string.IsNullOrWhiteSpace(queue)) throw new ArgumentException("A valid queue is required.");

            Id = Guid.NewGuid();
            Type = type;
            Message = message;
            Queue = queue;
            CreatedOn = createdOn;
            ParentId = parentId;
        }

        public Guid Id { get; private set; }

        public string Type { get; private set; }

        public string Message { get; private set; }
        public string Queue { get; private set; }

        public WorkItemStatus Status { get; private set; }

        public DateTime CreatedOn { get; private set; }

        public int Version { get; private set; }

        public int NewVersion { get; private set; }

        public int DispatchCount { get; private set; }

        public DateTime? RetryOn { get; private set; }

        public Guid? ParentId { get; private set; }

        public bool Running()
        {
            if (Status != WorkItemStatus.Ready)
                return false;

            Status = WorkItemStatus.Running;
            NextVersion();            
            DispatchCount++;

            return true;
        }

        public void Ready()
        {
            if (Status == WorkItemStatus.Completed || Status == WorkItemStatus.Poisoned)
                throw new InvalidOperationException(string.Format("Could not change a Completed or Poisoned work item to Ready. Current status is {0}", Status));

            RetryOn = null;
            Status = WorkItemStatus.Ready;
            NextVersion();            
        }

        public void Complete()
        {
            EnsureRunning();

            Status = WorkItemStatus.Completed;
            NextVersion();
        }

        public void Poison()
        {
            EnsureRunning();
            Status = WorkItemStatus.Poisoned;
            NextVersion();
        }

        public void Fail(DateTime retryOn)
        {
            EnsureRunning();

            Status = WorkItemStatus.Failed;
            NextVersion();
            RetryOn = retryOn;
        }


        void EnsureRunning()
        {
            if (Status != WorkItemStatus.Running)
                throw new InvalidOperationException("Could not complete a work item that is not in Running state.");
        }

        void NextVersion()
        {
            Version = NewVersion;
            NewVersion++;
        }

        public override string ToString()
        {
            return string.Format("Id: {0}, Version {1}, Type: {2}, DispatchCount: {3}, Status: {4}", Id, Version, Type, DispatchCount, Status);
        }
    }
}