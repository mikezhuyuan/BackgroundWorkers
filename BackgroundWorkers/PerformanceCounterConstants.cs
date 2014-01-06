namespace BackgroundWorkers
{
    public static class PerformanceCounterConstants
    {
        public const string Category = "Background Workers";
        public const string NewWorkItemsDispatcherThroughputCounter = "New work items/sec";
        public const string PoisonedWorkItemsDispatcherThroughputCounter = "Poisoned work items/sec";
        public const string WorkItemDispatcherThroughputCounterFormat = "Work items - {0}/sec";
        public const string HandlerCountFormat = "Active {0} handlers count";
    }
}