using System;

namespace BackgroundWorkers
{
    public class ConsoleLogger : ILogger
    {
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
            lock (Console.Out)
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