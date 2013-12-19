using Xunit;

namespace BackgroundWorkers.Tests
{
    public class WorkItemMessage
    {
        public int Id { get; set; }    
    }

    public class MessageFormatterFacts
    {
        [Fact]
        public void PreservesTypeInformation()
        {
            var f = new MessageFormatter();
            var i = new WorkItemMessage
            {
                Id = 10
            };

            var o = f.Deserialize(f.Serialize(i));


            Assert.NotSame(i, o);
            Assert.IsType<WorkItemMessage>(o);
            Assert.Equal(i.Id, ((WorkItemMessage)o).Id);
        }
    }
}