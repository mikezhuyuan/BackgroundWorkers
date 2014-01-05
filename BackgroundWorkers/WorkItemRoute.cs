using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackgroundWorkers
{
    public class WorkItemRouteData
    {
        public QueueConfiguration Config { get; set; }
        public ISendMessage<Guid> Client { get; set; }
    }

    public class WorkItemRoute
    {
        IEnumerable<ISendMessage<Guid>> _allClients;
        IDictionary<Type, List<ISendMessage<Guid>>> _lookup = new Dictionary<Type, List<ISendMessage<Guid>>>();
        public WorkItemRoute(IEnumerable<WorkItemRouteData> routeTable)
        {
            _allClients = routeTable.Select(r => r.Client);
            foreach (var route in routeTable)
            {
                var types = route.Config.MessageTypes;
                foreach (var type in types)
                {
                    if (!_lookup.ContainsKey(type))
                    {
                        _lookup[type] = new List<ISendMessage<Guid>>();
                    }

                    _lookup[type].Add(route.Client);
                }
            }
        } 

        public IEnumerable<ISendMessage<Guid>> GetRouteTargets(Type messageType)
        {
            List<ISendMessage<Guid>> result;

            if (!_lookup.TryGetValue(messageType, out result))
            {
                return _allClients;
            }

            return result;
        }
    }
}
