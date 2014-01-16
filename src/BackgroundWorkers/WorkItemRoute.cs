using System;
using System.Collections.Generic;

namespace BackgroundWorkers
{
    public class WorkItemRouteData
    {
        public QueueConfiguration Config { get; set; }
        public ISendMessage<Guid> Client { get; set; }        
    }

    public class WorkItemRoute
    {
        IDictionary<Type, ICollection<ISendMessage<Guid>>> _whiteList = new Dictionary<Type, ICollection<ISendMessage<Guid>>>();
        IDictionary<Type, ICollection<ISendMessage<Guid>>> _blackList = new Dictionary<Type, ICollection<ISendMessage<Guid>>>();
        ICollection<ISendMessage<Guid>> _listenToAllList = new HashSet<ISendMessage<Guid>>();

        public WorkItemRoute(IEnumerable<WorkItemRouteData> routeTable)
        {
            foreach (var route in routeTable)
            {
                if (route.Config.IsListenToAll)
                {
                     _listenToAllList.Add(route.Client);
                }

                foreach (var type in route.Config.MessageWhilteList)
                {
                    if (!_whiteList.ContainsKey(type))
                    {
                        _whiteList[type] = new HashSet<ISendMessage<Guid>>();
                    }

                    _whiteList[type].Add(route.Client);
                }

                foreach (var type in route.Config.MessageBlackList)
                {
                    if (!_blackList.ContainsKey(type))
                    {
                        _blackList[type] = new HashSet<ISendMessage<Guid>>();
                    }

                    _blackList[type].Add(route.Client);
                }
            }
        } 

        public IEnumerable<ISendMessage<Guid>> GetRouteTargets(Type messageType)
        {
            var result = new HashSet<ISendMessage<Guid>>(_listenToAllList);

            if (_whiteList.ContainsKey(messageType))
            {
                result.UnionWith(_whiteList[messageType]);
            }

            if (_blackList.ContainsKey(messageType))
            {
                result.ExceptWith(_blackList[messageType]);
            }

            return result;
        }
    }
}
