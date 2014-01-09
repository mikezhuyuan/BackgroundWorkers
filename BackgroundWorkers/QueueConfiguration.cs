using System;
using System.Collections.Generic;

namespace BackgroundWorkers
{
    public class QueueConfiguration
    {
        readonly HashSet<Type> _messageWhilteList = new HashSet<Type>();
        readonly HashSet<Type> _messageBlackList = new HashSet<Type>();

        public QueueConfiguration(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A valid name is required.");

            Name = name;
        }

        public string Name { get; set; }

        public int MaxWorkers { get; set; }

        public TimeSpan RetryDelay { get; set; }

        public int RetryCount { get; set; }

        public QueueConfiguration ListenTo(Type messageType)
        {
            _messageWhilteList.Add(messageType);

            return this;
        }

        public QueueConfiguration Except(Type messageType)
        {
            _messageBlackList.Add(messageType);

            return this;
        }

        public QueueConfiguration ListenToAll()
        {
            IsListenToAll = true;
            return this;
        }

        internal IEnumerable<Type> MessageWhilteList
        {
            get { return _messageWhilteList; }
        }

        internal IEnumerable<Type> MessageBlackList
        {
            get { return _messageBlackList; }
        }

        internal bool IsListenToAll { get; set; }
    }

    public static class QueueConfigurationExtentions
    {
        public static QueueConfiguration ListenTo<T>(this QueueConfiguration config)
        {
            return config.ListenTo(typeof(T));
        }

        public static QueueConfiguration Except<T>(this QueueConfiguration config)
        {
            return config.Except(typeof(T));
        }
    }
}