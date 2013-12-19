namespace BackgroundWorkers.Persistence
{
    public class InMemoryWorkItemRepositoryProvider : IWorkItemRepositoryProvider
    {
        public IWorkItemRepository Create()
        {
            return new InMemoryWorkItemRepository();
        }
    }
}