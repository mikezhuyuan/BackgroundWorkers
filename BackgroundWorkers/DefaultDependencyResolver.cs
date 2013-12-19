namespace BackgroundWorkers
{
    public class DefaultDependencyResolver : IDependencyResolver
    {
        public void Dispose()
        {
            
        }

        public IDependencyScope BeginScope()
        {
            return new DefaultDependencyScope();
        }
    }
}