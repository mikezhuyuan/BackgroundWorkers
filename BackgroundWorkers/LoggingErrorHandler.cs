using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace BackgroundWorkers
{
    public class LoggingErrorHandler : IErrorHandler
    {
        readonly ILogger _logger;

        public LoggingErrorHandler(ILogger logger)
        {
            if (logger == null) throw new ArgumentNullException("logger");
            _logger = logger;
        }

        public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {
        }

        public bool HandleError(Exception error)
        {
            _logger.Exception(error);
            return false;
        }
    }
}