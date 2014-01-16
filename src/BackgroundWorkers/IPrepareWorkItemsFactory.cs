namespace BackgroundWorkers
{
    public interface IPrepareWorkItemsFactory<in T>
    {
        IPrepareWorkItems<T> Create();
    }
}