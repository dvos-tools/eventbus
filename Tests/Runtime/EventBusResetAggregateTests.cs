using System;
using com.DvosTools.bus.Dispatchers;
using NUnit.Framework;

namespace com.DvosTools.bus
{
    [TestFixture]
    public class EventBusResetAggregateTests
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
        public void ResetAggregate_RemovesAllHandlersForAggregate()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var globalHandlerCalled = false;
            var aggregateHandlerCalled = false;
            EventBus.RegisterHandler<TestEvent>(evt => globalHandlerCalled = true);
            EventBus.RegisterHandler<TestEvent>(evt => aggregateHandlerCalled = true, aggregateId);

            // Act
            EventBus.ResetAggregate(aggregateId);

            // Assert
            Assert.IsTrue(EventBus.HasHandlers<TestEvent>()); // Global handler should remain
            Assert.AreEqual(1, EventBus.GetHandlerCount<TestEvent>());
            Assert.AreEqual(0, EventBus.GetHandlerCountForAggregate(aggregateId));
        }

        [Test]
        public void ResetAggregate_ClearsQueuedEventsForAggregate()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            EventBus.RegisterHandler<TestEvent>(evt => { }, aggregateId);
            var routedEvent1 = new RoutableTestEvent { Data = "Test1", AggregateId = aggregateId };
            var routedEvent2 = new RoutableTestEvent { Data = "Test2", AggregateId = aggregateId };
            EventBus.Send(routedEvent1);
            EventBus.Send(routedEvent2);

            // Act
            EventBus.ResetAggregate(aggregateId);

            // Assert
            Assert.AreEqual(0, EventBus.GetQueueCount());
        }

        [Test]
        public void ResetAggregate_ClearsBufferedEventsForAggregate()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var routedEvent = new RoutableTestEvent { Data = "Buffered", AggregateId = aggregateId };
            EventBus.Send(routedEvent); // This will be buffered

            // Act
            EventBus.ResetAggregate(aggregateId);

            // Assert
            Assert.AreEqual(0, EventBus.GetBufferedEventCount(aggregateId));
            Assert.AreEqual(0, EventBus.GetTotalBufferedEventCount());
        }

        [Test]
        public void ResetAggregate_WithMultipleEventTypes_RemovesAllForAggregate()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var testEventHandlerCalled = false;
            var anotherEventHandlerCalled = false;
            EventBus.RegisterHandler<TestEvent>(evt => testEventHandlerCalled = true, aggregateId);
            EventBus.RegisterHandler<AnotherTestEvent>(evt => anotherEventHandlerCalled = true, aggregateId);

            // Act
            EventBus.ResetAggregate(aggregateId);

            // Assert
            Assert.AreEqual(0, EventBus.GetHandlerCountForAggregate(aggregateId));
            Assert.IsFalse(EventBus.HasHandlersForAggregate(aggregateId));
        }

        [Test]
        public void ResetAggregate_WithEmptyGuid_DoesNothing()
        {
            // Arrange
            EventBus.RegisterHandler<TestEvent>(evt => { }, Guid.Empty);

            // Act
            EventBus.ResetAggregate(Guid.Empty);

            // Assert
            // ResetAggregate with Guid.Empty should do nothing to avoid clearing all global handlers
            Assert.AreEqual(1, EventBus.GetHandlerCountForAggregate(Guid.Empty));
        }

        [Test]
        public void ResetAggregate_WithNonExistentAggregate_DoesNotThrow()
        {
            // Arrange
            var nonExistentAggregateId = Guid.NewGuid();

            // Act & Assert
            Assert.DoesNotThrow(() => EventBus.ResetAggregate(nonExistentAggregateId));
        }
    }
}