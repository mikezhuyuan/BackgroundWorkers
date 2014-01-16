using System;
using System.Collections.Generic;
using Autofac;

namespace BackgroundWorkers.Integration.Autofac
{
    public class AutofacDependencyScope : IDependencyScope
    {
        readonly ILifetimeScope _scope;

        public AutofacDependencyScope(ILifetimeScope scope)
        {
            if (scope == null) throw new ArgumentNullException("scope");
            _scope = scope;
        }

        public void Dispose()
        {            
            _scope.Dispose();
        }

        public object GetService(Type type)
        {
            return _scope.Resolve(type);
        }

        public IEnumerable<object> GetServices(Type type)
        {
            var s = _scope.Resolve(type);
            return s as IEnumerable<object> ?? new[] {s};
        }

        public bool TryGetService(Type type, out object service)
        {
            return _scope.TryResolve(type, out service);
        }
    }
}