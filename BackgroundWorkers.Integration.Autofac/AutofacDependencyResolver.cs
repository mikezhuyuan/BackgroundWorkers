using System;
using Autofac;

namespace BackgroundWorkers.Integration.Autofac
{
    public class AutofacDependencyResolver : IDependencyResolver
    {
        readonly ILifetimeScope _scope;

        public AutofacDependencyResolver(ILifetimeScope scope)
        {
            if (scope == null) throw new ArgumentNullException("scope");
            _scope = scope;
        }

        public IDependencyScope BeginScope()
        {
            return new AutofacDependencyScope(_scope.BeginLifetimeScope());
        }

        public void Dispose()
        {
            _scope.Dispose();
        }
    }
}