using System;
using System.Diagnostics;

namespace BackgroundWorkers
{
    public class ConsoleLogger : ILogger
    {
        static readonly object _sync = new object();

        public void Information(string message, params object[] args)
        {
            WriteLine(ConsoleColor.Cyan, message, args);
        }

        public void Exception(Exception exception)
        {
            WriteLine(ConsoleColor.Magenta, exception.ToString() ?? exception.GetType().ToString());
        }

        public void Warning(string message, params object[] args)
        {
            WriteLine(ConsoleColor.Yellow, message, args);
        }

        static void WriteLine(ConsoleColor color, string message, params object[] args)
        {
            lock (_sync)
            {
                var currentColor = Console.ForegroundColor;
                try
                {
                    Console.ForegroundColor = color;
                    Console.WriteLine(message, args);
                }
                finally
                {
                    Console.ForegroundColor = currentColor;
                }
            }            
        }
    }
}