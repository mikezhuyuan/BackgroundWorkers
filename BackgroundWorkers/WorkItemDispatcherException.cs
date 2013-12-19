using System;

namespace BackgroundWorkers
{
    public class WorkItemDispatcherException : Exception
    {
        public WorkItemDispatcherException(string message, Exception innerException) : base(message, innerException)
        {            
        }
    }
}