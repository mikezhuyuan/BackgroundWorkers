using System;

namespace BackgroundWorkers
{
    public class QueueConfiguration
    {
        public QueueConfiguration(string name, int maxWorkers = 0)
        {
            Name = name;
            MaxWorkers = maxWorkers;

            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A valid name is required.");
        }

        public string Name { get; set; }

        public int MaxWorkers { get; set; }
    }
}