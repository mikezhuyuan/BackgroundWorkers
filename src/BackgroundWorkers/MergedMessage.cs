using System.Collections.Generic;
using System.Linq;

namespace BackgroundWorkers
{
    public class MergedMessages<T>
    {
        public IEnumerable<T> Messages { get; set; }

        internal void SetMessages(IEnumerable<object> source)
        {
            Messages = source.OfType<T>();
        }
    }
}
