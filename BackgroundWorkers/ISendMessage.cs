namespace BackgroundWorkers
{
    public interface ISendMessage<in T>
    {
        void Send(T message);
        string Queue { get; }
    }
}