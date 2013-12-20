using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
#if(DEBUG)
            RetryDelay = TimeSpan.FromSeconds(10);
            RetryCount = 2;
#else
            RetryDelay = TimeSpan.FromMinutes(5);            
            RetryCount = 5;
#endif
            NewWorkItemQueue = new QueueConfiguration("BackgroundWorkersNewWorkItemsQueue");
            PoisonedWorkItemQueue = new QueueConfiguration("BackgroundWorkersPoisonedWorkItemsQueue");
        }

        public IMessageFormatter MessageFormatter { get; private set; }

        public ILogger Logger { get; private set; }

        public IDependencyResolver DependencyResolver { get; private set; }

        public Func<DateTime> Now { get; private set; }

        public IWorkItemRepositoryProvider WorkItemRepositoryProvider { get; private set; }

        public Collection<QueueConfiguration> WorkItemQueues { get; private set; }

        public QueueConfiguration NewWorkItemQueue { get; private set; }

        public QueueConfiguration PoisonedWorkItemQueue { get; private set; }

        public TimeSpan RetryDelay { get; private set; }

        public int RetryCount { get; private set; }

        public Host CreateHost()
        {
            EnsureQueues(WorkItemQueues.Select(q => q.Name).Concat(new[] { NewWorkItemQueue.Name, PoisonedWorkItemQueue.Name }));

            var widFactories = WorkItemQueues.ToDictionary(qc => qc, qc => new WorkItemDispatcherFactory(this));
            var nwidFactory = new NewWorkItemDispatcherFactory(this);
            var pwidFactory = new PoisonedWorkItemDispatcherFactory(this);

            var clients = WorkItemQueues.Select(MsmqHelpers.CreateQueue<Guid>).ToArray();

            return new Host(
                WorkItemQueues.Select(q => new MsmqListener<Guid>(MsmqHelpers.CreateNativeQueue(q), widFactories[q].Create, Logger, q.MaxWorkers)),
                new MsmqListener<NewWorkItem>(MsmqHelpers.CreateNativeQueue(NewWorkItemQueue), nwidFactory.Create, Logger, NewWorkItemQueue.MaxWorkers),
                new MsmqListener<Guid>(MsmqHelpers.CreateNativeQueue(PoisonedWorkItemQueue), pwidFactory.Create, Logger, PoisonedWorkItemQueue.MaxWorkers),
                new RetryClock(RetryDelay, Logger, WorkItemRepositoryProvider, Now, clients),
                new IncompleteWork(WorkItemRepositoryProvider, clients));
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

        public WorkersConfiguration WithQueue(string name, int maxWorkers = 1)
        {
            WorkItemQueues.Add(new QueueConfiguration(name, maxWorkers));
            return this;
        }

        public WorkersConfiguration WithRetryDelay(TimeSpan delay)
        {
            RetryDelay = RetryDelay;
            return this;
        }

        public WorkersConfiguration WithRetryCount(int count)
        {
            RetryCount = count;
            return this;
        }

        public static WorkersConfiguration Current
        {
            get { return CurrentConfig; }
        }
    }
}