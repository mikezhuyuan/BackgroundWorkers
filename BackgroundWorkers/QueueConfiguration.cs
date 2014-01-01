using System;

namespace BackgroundWorkers
{
    public class QueueConfiguration
    {
        public QueueConfiguration(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A valid name is required.");

            Name = name;
        }

        public string Name { get; set; }

        public int MaxWorkers { get; set; }

        public TimeSpan RetryDelay { get; set; }

        public int RetryCount { get; set; }

    }
}