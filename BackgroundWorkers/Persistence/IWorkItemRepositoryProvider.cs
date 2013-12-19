namespace BackgroundWorkers.Persistence
{
    public interface IWorkItemRepositoryProvider
    {
        IWorkItemRepository Create();
    }
}