using System;

namespace BackgroundWorkers.Demo
{
    class Logger : ConsoleLogger, ILogger
    {
        public new void Information(string message, params object[] args)
        {
            base.Information(message, args);
            AppHub.Log(string.Format(message, args));
        }

        public new void Exception(Exception exception)
        {
            base.Exception(exception);
            AppHub.Log(exception.ToString());
        }

        public new void Warning(string message, params object[] args)
        {
            base.Warning(message, args);
            AppHub.Log(string.Format(message, args));
        }
    }
}
