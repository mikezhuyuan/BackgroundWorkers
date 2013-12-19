using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Messaging;
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
            Queues = new Collection<string>();
#if(DEBUG)
            RetryDelay = TimeSpan.FromSeconds(10);
            RetryCount = 2;
#else
            RetryDelay = TimeSpan.FromMinutes(5);            
            RetryCount = 5;
#endif
            WorkItemQueueName = "BackgroundWorkersWorkItemQueue";
            PoisonedWorkItemQueueName = "BackgroundWorkersPoisonedWorkItemQueue";
        }

        public string PoisonedWorkItemQueueName { get; private set; }

        public IMessageFormatter MessageFormatter { get; private set; }

        public ILogger Logger { get; private set; }

        public IDependencyResolver DependencyResolver { get; private set; }

        public Func<DateTime> Now { get; private set; }

        public IWorkItemRepositoryProvider WorkItemRepositoryProvider { get; private set; }

        public Collection<string> Queues { get; private set; }

        public TimeSpan RetryDelay { get; private set; }

        public int RetryCount { get; private set; }

        public string WorkItemQueueName { get; private set; }

        public Host CreateHost()
        {
            EnsureQueues();

            var clients = Queues.Select(q => new MsmqQueue<Guid>(new MessageQueue(MsmqHelpers.PrivateQueueUri(q)))).ToArray();

            return new Host(
                Queues.Select(q => new MsmqListener<Guid>(new MessageQueue(MsmqHelpers.PrivateQueueUri(q)), () => new WorkItemDispatcher(DependencyResolver, WorkItemRepositoryProvider, WorkItemRepositoryProvider.Create(), new WorkItemQueueClient(new MsmqQueue<NewWorkItem>(new MessageQueue(MsmqHelpers.PrivateQueueUri(WorkItemQueueName))), MessageFormatter, Now), MessageFormatter, new ErrorHandlingPolicy(new MsmqQueue<Guid>(new MessageQueue(MsmqHelpers.PrivateQueueUri(PoisonedWorkItemQueueName))), WorkItemRepositoryProvider, Now, Logger, RetryCount), Logger), Logger, 1)),
                new MsmqListener<Guid>(new MessageQueue(MsmqHelpers.PrivateQueueUri(PoisonedWorkItemQueueName)), () => new PoisonedWorkItemDispatcher(WorkItemRepositoryProvider.Create(), DependencyResolver.BeginScope(), MessageFormatter, Logger), Logger, 1),
                new MsmqListener<NewWorkItem>(new MessageQueue(MsmqHelpers.PrivateQueueUri(WorkItemQueueName)), () => new WorkItemQueue(MessageFormatter, WorkItemRepositoryProvider.Create(), clients, Logger), Logger, 1),
                new RetryClock(RetryDelay, Logger, WorkItemRepositoryProvider, Now, clients),
                new IncompleteWork(WorkItemRepositoryProvider, clients));
        }

        void EnsureQueues()
        {
            foreach (var q in Queues.Concat(new [] { WorkItemQueueName, PoisonedWorkItemQueueName }))
            {
                MsmqHelpers.EnsureQueueExists(q);
            }            
        }

        public IWorkItemQueueClient CreateClient()
        {
            return new WorkItemQueueClient(new MsmqQueue<NewWorkItem>(new MessageQueue(MsmqHelpers.PrivateQueueUri(WorkItemQueueName))), MessageFormatter, Now);
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

        public Configuration WithQueue(string name)
        {
            Queues.Add(name);
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