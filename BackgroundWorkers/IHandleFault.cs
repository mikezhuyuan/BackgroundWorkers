namespace BackgroundWorkers
{   
    public interface IHandleFault<in T>
    {
        void Run(T message);
    }
}