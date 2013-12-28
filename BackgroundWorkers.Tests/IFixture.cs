namespace BackgroundWorkers.Tests
{
    public interface IFixture<out T>
    {
        T Subject { get; }
    }
}