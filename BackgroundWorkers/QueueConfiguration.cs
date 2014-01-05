using System;
using System.Collections.Generic;

namespace BackgroundWorkers
{
    public class QueueConfiguration
    {
        readonly List<Type> _messageTypes = new List<Type>(); 
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
            if (!_messageTypes.Contains(messageType))
                _messageTypes.Add(messageType);

            return this;
        }

        //TODO: ListenToAll, ExcludeListenTo

        public IEnumerable<Type> MessageTypes
        {
            get { return _messageTypes; }
        }
    }

    public static class QueueConfigurationExtentions
    {
        public static QueueConfiguration ListenTo<T>(this QueueConfiguration config)
        {
            return config.ListenTo(typeof(T));
        }
    }
}