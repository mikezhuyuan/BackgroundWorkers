using System;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace BackgroundWorkers
{
    public static class ExceptionExtensions
    {
        public static bool IsFatal(this Exception exception)
        {
            for (; exception != null; exception = exception.InnerException)
            {
                if (exception is OutOfMemoryException && !(exception is InsufficientMemoryException) ||
                    (exception is ThreadAbortException))
                {
                    return true;
                }

                if (exception is TypeInitializationException || exception is TargetInvocationException)
                    continue;

                var aggregateException = exception as AggregateException;
                if (aggregateException != null)
                {
                    if (aggregateException.InnerExceptions.Any(IsFatal))
                        return true;
                }

                break;
            }

            return false;
        }
    }
}