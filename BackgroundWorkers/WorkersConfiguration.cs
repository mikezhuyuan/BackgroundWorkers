using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using BackgroundWorkers.Persistence;

namespace BackgroundWorkers
{
    public class WorkersConfiguration
    {
        static readonly WorkersConfiguration CurrentConfig = new WorkersConfiguration();

        public WorkersConfiguration()
        {
            MessageFormatter = new MessageFormatter();
            Logger = new ConsoleLogger();
            DependencyResolver = new DefaultDependencyResolver();
            Now = () => DateTime.Now;
            WorkItemRepositoryProvider = new InMemoryWorkItemRepositoryProvider();
            WorkItemQueues = new Collection<QueueConfiguration>();
            NewWorkItemQueue = new QueueConfiguration("BackgroundWorkersNewWorkItemsQueue");
            PoisonedWorkItemQueue = new QueueConfiguration("BackgroundWorkersPoisonedWorkItemsQueue");
            RetryClockFrequency = TimeSpan.FromMinutes(1);
        }

        public IMessageFormatter MessageFormatter { get; private set; }

        public ILogger Logger { get; private set; }

        public IDependencyResolver DependencyResolver { get; private set; }

        public Func<DateTime> Now { get; private set; }

        public IWorkItemRepositoryProvider WorkItemRepositoryProvider { get; private set; }

        public Collection<QueueConfiguration> WorkItemQueues { get; private set; }

        public QueueConfiguration NewWorkItemQueue { get; private set; }

        public QueueConfiguration PoisonedWorkItemQueue { get; private set; }

        public TimeSpan RetryClockFrequency { get; set; }

        public WorkersHost CreateHost()
        {
            EnsureQueues(WorkItemQueues.Select(q => q.Name).Concat(new[] { NewWorkItemQueue.Name, PoisonedWorkItemQueue.Name }));

            var widFactories = WorkItemQueues.ToDictionary(qc => qc, qc => new WorkItemDispatcherFactory(this, qc));
            var nwidFactory = new NewWorkItemDispatcherFactory(this);
            var pwidFactory = new PoisonedWorkItemDispatcherFactory(this);

            var clients = WorkItemQueues.Select(MsmqHelpers.CreateQueue<Guid>).ToArray();

            return new WorkersHost(
                WorkItemQueues.Select(q => new MsmqListener<Guid>(q.Name, MsmqHelpers.CreateNativeQueue(q), widFactories[q].Create, Logger, q.MaxWorkers)),
                new MsmqListener<NewWorkItem>(NewWorkItemQueue.Name, MsmqHelpers.CreateNativeQueue(NewWorkItemQueue), nwidFactory.Create, Logger, NewWorkItemQueue.MaxWorkers),
                new MsmqListener<Guid>(PoisonedWorkItemQueue.Name, MsmqHelpers.CreateNativeQueue(PoisonedWorkItemQueue), pwidFactory.Create, Logger, PoisonedWorkItemQueue.MaxWorkers),
                new RetryClock(RetryClockFrequency, Logger, WorkItemRepositoryProvider, Now, clients),
                new IncompleteWork(WorkItemRepositoryProvider, clients));
        }
       
        public void CreatePerformanceCounters()
        {
            var ccdc = new CounterCreationDataCollection();

            var newWorkItemsDispatcherThroughput = new CounterCreationData(string.Format("{0}/sec", NewWorkItemQueue.Name),
                "Number of new work items processed per second", PerformanceCounterType.RateOfCountsPerSecond64);

            var poisonedWorkItemsDispatchThroughput = new CounterCreationData(string.Format("{0}/sec", PoisonedWorkItemQueue.Name),
                "Number of poisoned work items processed per second", PerformanceCounterType.RateOfCountsPerSecond64);


            var dispatcherCounters =
                WorkItemQueues.SelectMany(WorkItemDispatcherCounters)
                .ToArray();

            ccdc.Add(newWorkItemsDispatcherThroughput);
            ccdc.Add(poisonedWorkItemsDispatchThroughput);
            ccdc.AddRange(dispatcherCounters);

            DeletePerformanceCounters();
            
            PerformanceCounterCategory.Create(PerformanceCounterConstants.Category, "Background Workers Counters",
                PerformanceCounterCategoryType.SingleInstance, ccdc);
        }

        public void DeletePerformanceCounters()
        {
            if (PerformanceCounterCategory.Exists(PerformanceCounterConstants.Category))
                PerformanceCounterCategory.Delete(PerformanceCounterConstants.Category);

        }

        IEnumerable<CounterCreationData> WorkItemDispatcherCounters(QueueConfiguration configuration)
        {
            yield return
                new CounterCreationData(
                    string.Format("{0}/sec", configuration.Name),
                    string.Format("Number of work items processed per second - {0}", configuration.Name), PerformanceCounterType.RateOfCountsPerSecond64);

            yield return
                new CounterCreationData(
                    string.Format("{0} count",
                        configuration.Name),
                    string.Format("Number of active {0} handlers", configuration.Name), PerformanceCounterType.NumberOfItems64);
        }

        static void EnsureQueues(IEnumerable<string> names)
        {
            foreach (var q in names)
            {
                MsmqHelpers.EnsureQueueExists(q);
            }
        }

        public IWorkItemQueueClient CreateClient()
        {
            return new WorkItemQueueClient(new MsmqQueue<NewWorkItem>(MsmqHelpers.CreateNativeQueue(NewWorkItemQueue)), MessageFormatter, Now);
        }


        public WorkersConfiguration UseLogger(ILogger logger)
        {
            if (logger == null) throw new ArgumentNullException("logger");
            Logger = logger;
            return this;
        }

        public WorkersConfiguration UseFormatter(IMessageFormatter formatter)
        {
            if (formatter == null) throw new ArgumentNullException("formatter");
            MessageFormatter = formatter;
            return this;
        }

        public WorkersConfiguration UseDependencyResolver(IDependencyResolver dependencyResolver)
        {
            if (dependencyResolver == null) throw new ArgumentNullException("dependencyResolver");
            DependencyResolver = dependencyResolver;
            return this;
        }

        public WorkersConfiguration UseWorkItemRepositoryProvider(IWorkItemRepositoryProvider provider)
        {
            if (provider == null) throw new ArgumentNullException("provider");
            WorkItemRepositoryProvider = provider;
            return this;
        }

        public WorkersConfiguration UseNewWorkItemsQueueName(string name)
        {
            NewWorkItemQueue.Name = name;
            return this;
        }

        public WorkersConfiguration UsePoisonedWorkItemsQueueName(string name)
        {
            PoisonedWorkItemQueue.Name = name;
            return this;
        }

        public WorkersConfiguration WithQueue(string name, Action<QueueConfiguration> configuration)
        {
            var c = new QueueConfiguration(name);
            configuration(c);
            WorkItemQueues.Add(c);
            return this;
        }

        public WorkersConfiguration WithRetryClockFrequency(TimeSpan delay)
        {
            RetryClockFrequency = delay;
            return this;
        }

        public static WorkersConfiguration Current
        {
            get { return CurrentConfig; }
        }
    }
}