using System;
using System.Collections.Generic;
using System.Linq;

namespace BackgroundWorkers.Demo
{
    public static class BlackList
    {
        const int MaxCount = 1024;
        static object _lock = new object();
        static HashSet<string> _blacklist = new HashSet<string>();
        public static bool ShouldVisit(string url)
        {
            lock (_lock)
            {
                return !_blacklist.Contains(url);
            }
        }

        public static void Block(string url)
        {
            if (_blacklist.Count > MaxCount)
            {
                lock (_lock)
                {
                    var array = new string[_blacklist.Count / 2];
                    _blacklist.CopyTo(array, 0, _blacklist.Count / 2);
                    _blacklist = new HashSet<string>(array);
                }
            }

            lock (_lock)
            {
                _blacklist.Add(url);
            }
        }
    }
}
