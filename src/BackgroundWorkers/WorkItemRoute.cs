using System;
using System.Collections.Generic;
using System.Linq;

namespace BackgroundWorkers
{
    public class WorkItemRouteData
    {
        public QueueConfiguration Config { get; set; }
        public ISendMessage<Guid> Client { get; set; }        
    }

    public class WorkItemRoute
    {
        readonly ISendMessage<Guid> _mergeClient;
        IDictionary<Type, ICollection<ISendMessage<Guid>>> _whiteList = new Dictionary<Type, ICollection<ISendMessage<Guid>>>();
        IDictionary<Type, ICollection<ISendMessage<Guid>>> _blackList = new Dictionary<Type, ICollection<ISendMessage<Guid>>>();
        ICollection<ISendMessage<Guid>> _listenToAllList = new HashSet<ISendMessage<Guid>>();
        private List<WorkItemRouteData> routeTable;
        private QueueConfiguration queueConfiguration;

        public WorkItemRoute(IEnumerable<WorkItemRouteData> routeTable, ISendMessage<Guid> mergeClient)
        {
            if (routeTable == null) throw new ArgumentNullException("routeTable");
            if (mergeClient == null) throw new ArgumentNullException("mergeClient");

            _mergeClient = mergeClient;
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
            if (messageType == typeof (MergeableMessage))
            {
                return new[] {_mergeClient};
            }

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
