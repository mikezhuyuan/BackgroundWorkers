using System;

namespace BackgroundWorkers
{
    public interface ILogger
    {
        void Information(string message, params object[] args);

        void Exception(Exception exception);

        void Warning(string message, params object[] args);
    }
}