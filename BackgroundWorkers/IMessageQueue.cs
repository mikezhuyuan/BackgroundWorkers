namespace BackgroundWorkers
{
    public interface IMessageQueue<in T>
    {
        void Send(T message);
        string Queue { get; }
    }
}