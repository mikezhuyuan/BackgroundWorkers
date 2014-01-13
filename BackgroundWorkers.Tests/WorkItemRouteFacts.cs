using System;
using System.Linq;
using NSubstitute;
using Xunit;

namespace BackgroundWorkers.Tests
{
    public class WorkItemRouteFacts
    {
        [Fact]
        public void ShouldNotListenToAnyMessageByDefault()
        {
            var client1 = Substitute.For<ISendMessage<Guid>>();

            var routeTable = new[]
            {
                new WorkItemRouteData
                {
                    Client = client1,
                    Config = new QueueConfiguration("q1")
                },
            };

            var route = new WorkItemRoute(routeTable);
            var result = route.GetRouteTargets(typeof(Object));

            Assert.Equal(0, result.Count());
        }

        [Fact]
        public void ShouldListenToAll()
        {
            var client1 = Substitute.For<ISendMessage<Guid>>();

            var routeTable = new[]
            {
                new WorkItemRouteData
                {
                    Client = client1,
                    Config = new QueueConfiguration("q1").ListenToAll()
                },
            };

            var route = new WorkItemRoute(routeTable);
            var result = route.GetRouteTargets(typeof(Object));

            Assert.Equal(client1, result.Single());
        }

        [Fact]
        public void ShouldListenToSpecificMessages()
        {
            var client1 = Substitute.For<ISendMessage<Guid>>();
            var client2 = Substitute.For<ISendMessage<Guid>>();

            var routeTable = new[]
            {
                new WorkItemRouteData
                {
                    Client = client1,
                    Config = new QueueConfiguration("q1").ListenTo<string>()
                },
                new WorkItemRouteData
                {
                    Client = client2,
                    Config = new QueueConfiguration("q2").ListenTo<int>()
                },
            };

            var route = new WorkItemRoute(routeTable);

            var result = route.GetRouteTargets(typeof(string));
            Assert.Equal(client1, result.Single());

            result = route.GetRouteTargets(typeof(int));
            Assert.Equal(client2, result.Single());
        }

        [Fact]
        public void ShouldIgnoreMessages()
        {
            var client1 = Substitute.For<ISendMessage<Guid>>();

            var routeTable = new[]
            {
                new WorkItemRouteData
                {
                    Client = client1,
                    Config = new QueueConfiguration("q1").ListenToAll().Except(typeof(string))
                },
            };

            var route = new WorkItemRoute(routeTable);
            var result = route.GetRouteTargets(typeof(string));

            Assert.Equal(0, result.Count());
        }
    }
}
