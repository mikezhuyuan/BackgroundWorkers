using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BackgroundWorkers.Persistence;

namespace BackgroundWorkers
{
    public class Configuration
    {
        static readonly Configuration CurrentConfig = new Configuration();

        public Configuration()
        {
            MessageFormatter = new MessageFormatter();
            Logger = new ConsoleLogger();
            DependencyResolver = new DefaultDependencyResolver();
            Now = () => DateTime.Now;
            WorkItemRepositoryProvider = new InMemoryWorkItemRepositoryProvider();
            Queues = new Collection<QueueConfiguration>();
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

        public QueueConfiguration PoisonedWorkItemQueue { get; private set; }

        public IMessageFormatter MessageFormatter { get; private set; }

        public ILogger Logger { get; private set; }

        public IDependencyResolver DependencyResolver { get; private set; }

        public Func<DateTime> Now { get; private set; }

        public IWorkItemRepositoryProvider WorkItemRepositoryProvider { get; private set; }

        public Collection<QueueConfiguration> Queues { get; private set; }

        public TimeSpan RetryDelay { get; private set; }

        public int RetryCount { get; private set; }

        public QueueConfiguration NewWorkItemQueue { get; private set; }

        public Host CreateHost()
        {
            EnsureQueues(Queues.Select(q => q.Name).Concat(new[] { NewWorkItemQueue.Name, PoisonedWorkItemQueue.Name }));

            var widFactories = Queues.ToDictionary(qc => qc, qc => new WorkItemDispatcherFactory(this));
            var nwidFactory = new NewWorkItemDispatcherFactory(this);
            var pwidFactory = new PoisonedWorkItemDispatcherFactory(this);

            var clients = Queues.Select(MsmqHelpers.CreateQueue<Guid>).ToArray();

            return new Host(
                Queues.Select(q => new MsmqListener<Guid>(MsmqHelpers.CreateNativeQueue(q), widFactories[q].Create, Logger, q.MaxWorkers)),
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


        public Configuration UseLogger(ILogger logger)
        {
            if (logger == null) throw new ArgumentNullException("logger");
            Logger = logger;
            return this;
        }

        public Configuration UseFormatter(IMessageFormatter formatter)
        {
            if (formatter == null) throw new ArgumentNullException("formatter");
            MessageFormatter = formatter;
            return this;
        }

        public Configuration UseDependencyResolver(IDependencyResolver dependencyResolver)
        {
            if (dependencyResolver == null) throw new ArgumentNullException("dependencyResolver");
            DependencyResolver = dependencyResolver;
            return this;
        }

        public Configuration UseWorkItemRepositoryProvider(IWorkItemRepositoryProvider provider)
        {
            if (provider == null) throw new ArgumentNullException("provider");
            WorkItemRepositoryProvider = provider;
            return this;
        }

        public Configuration WithQueue(string name, int maxWorkers = 1)
        {
            Queues.Add(new QueueConfiguration(name, maxWorkers));
            return this;
        }

        public Configuration WithRetryDelay(TimeSpan delay)
        {
            RetryDelay = RetryDelay;
            return this;
        }

        public Configuration WithRetryCount(int count)
        {
            RetryCount = count;
            return this;
        }

        public static Configuration Current
        {
            get { return CurrentConfig; }
        }
    }
}