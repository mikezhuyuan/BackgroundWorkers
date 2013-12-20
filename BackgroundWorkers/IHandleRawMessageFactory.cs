namespace BackgroundWorkers
{
    public interface IHandleRawMessageFactory<in T>
    {
        IHandleRawMessage<T> Create();
    }
}