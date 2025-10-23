using System;
using com.DvosTools.bus.Dispatchers;
using NUnit.Framework;

namespace com.DvosTools.bus
{
    [TestFixture]
    public class EventBusDisposeAllHandlersTests
    {
        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ClearAll();
        }

        [Test]
        public void DisposeAllHandlers_RemovesAllHandlers()
        {
            // Arrange
            var handler1Called = false;
            var handler2Called = false;
            EventBus.RegisterHandler<TestEvent>(evt => handler1Called = true);
            EventBus.RegisterHandler<AnotherTestEvent>(evt => handler2Called = true);

            // Act
            EventBus.DisposeAllHandlers();

            // Assert
            Assert.IsFalse(EventBus.HasHandlers<TestEvent>());
            Assert.IsFalse(EventBus.HasHandlers<AnotherTestEvent>());
            Assert.AreEqual(0, EventBus.GetHandlerCount<TestEvent>());
            Assert.AreEqual(0, EventBus.GetHandlerCount<AnotherTestEvent>());
        }

        [Test]
        public void DisposeAllHandlers_ClearsAllQueuedEvents()
        {
            // Arrange
            EventBus.RegisterHandler<TestEvent>(evt => { });
            EventBus.Send(new TestEvent { Message = "Test1" });
            EventBus.Send(new TestEvent { Message = "Test2" });

            // Act
            EventBus.DisposeAllHandlers();

            // Assert
            Assert.AreEqual(0, EventBus.GetQueueCount());
            Assert.IsFalse(EventBus.HasQueuedEvents());
        }

        [Test]
        public void DisposeAllHandlers_ClearsAllBufferedEvents()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var routedEvent = new RoutableTestEvent { Data = "Buffered", AggregateId = aggregateId };
            EventBus.Send(routedEvent); // This will be buffered since no handler exists

            // Act
            EventBus.DisposeAllHandlers();

            // Assert
            Assert.AreEqual(0, EventBus.GetTotalBufferedEventCount());
            Assert.IsFalse(EventBus.HasBufferedEvents());
        }

        [Test]
        public void DisposeAllHandlers_WithAggregateHandlers_RemovesAllHandlers()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var globalHandlerCalled = false;
            var aggregateHandlerCalled = false;
            EventBus.RegisterHandler<TestEvent>(evt => globalHandlerCalled = true);
            EventBus.RegisterHandler<TestEvent>(evt => aggregateHandlerCalled = true, aggregateId);

            // Act
            EventBus.DisposeAllHandlers();

            // Assert
            Assert.IsFalse(EventBus.HasHandlers<TestEvent>());
            Assert.AreEqual(0, EventBus.GetHandlerCount<TestEvent>());
            Assert.AreEqual(0, EventBus.GetHandlerCountForAggregate(aggregateId));
        }

        [Test]
        public void DisposeAllHandlers_WithMixedEvents_ClearsEverything()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            EventBus.RegisterHandler<TestEvent>(evt => { });
            EventBus.RegisterHandler<TestEvent>(evt => { }, aggregateId);
            EventBus.Send(new TestEvent { Message = "Global" });
            EventBus.Send(new RoutableTestEvent { Data = "Aggregate", AggregateId = aggregateId });

            // Act
            EventBus.DisposeAllHandlers();

            // Assert
            Assert.AreEqual(0, EventBus.GetHandlerCount<TestEvent>());
            Assert.AreEqual(0, EventBus.GetQueueCount());
            Assert.AreEqual(0, EventBus.GetTotalBufferedEventCount());
        }
    }
}