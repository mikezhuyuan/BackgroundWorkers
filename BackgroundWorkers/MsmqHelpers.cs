using System;
using System.Messaging;
using System.ServiceModel;

namespace BackgroundWorkers
{
    public static class MsmqHelpers
    {
        public static void EnsureQueueExists(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A valid name is required.");

            var path = string.Format(".\\private$\\{0}", name);
            if (!MessageQueue.Exists(path))
            {
                MessageQueue.Create(path, true);
            }
        }

        public static string PrivateQueueUri(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A valid name is required.");

            return string.Format(".\\private$\\{0}", name);
        }

        public static NetMsmqBinding Binding
        {
            get { return new NetMsmqBinding {Security = {Mode = NetMsmqSecurityMode.None}}; }
        }

        public static MsmqQueue<T> CreateQueue<T>(QueueConfiguration configuration)
        {
            return new MsmqQueue<T>(CreateNativeQueue(configuration));
        }

        public static MessageQueue CreateNativeQueue(QueueConfiguration configuration)
        {
            return new MessageQueue(PrivateQueueUri(configuration.Name));
        }
    }
}