using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace BackgroundWorkers
{
    public class ServiceInstanceScopeExtension : IExtension<InstanceContext>, IDisposable
    {
        readonly IEnumerable<IDisposable> _disposables;

        public ServiceInstanceScopeExtension(IEnumerable<IDisposable> disposables)
        {
            if (disposables == null) throw new ArgumentNullException("disposables");
            _disposables = disposables;
        }

        public void Attach(InstanceContext owner)
        {
        }

        public void Detach(InstanceContext owner)
        {
        }


        public void Dispose()
        {
            foreach(var d in _disposables)
                d.Dispose();
        }
    }
}