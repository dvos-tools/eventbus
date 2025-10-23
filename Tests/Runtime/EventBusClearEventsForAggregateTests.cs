using System;
using NUnit.Framework;

namespace com.DvosTools.bus
{
    [TestFixture]
    public class EventBusClearEventsForAggregateTests
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
        public void ClearEventsForAggregate_ClearsQueuedEventsButKeepsHandlers()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            EventBus.RegisterHandler<TestEvent>(evt => _ = true, aggregateId);
            var routedEvent = new RoutableTestEvent { Data = "Test", AggregateId = aggregateId };
            EventBus.Send(routedEvent);

            // Act
            EventBus.ClearEventsForAggregate(aggregateId);

            // Assert
            Assert.AreEqual(0, EventBus.GetQueueCount());
            Assert.IsTrue(EventBus.HasHandlersForAggregate(aggregateId));
            Assert.AreEqual(1, EventBus.GetHandlerCountForAggregate(aggregateId));
        }

        [Test]
        public void ClearEventsForAggregate_ClearsBufferedEventsButKeepsHandlers()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            EventBus.RegisterHandler<TestEvent>(evt => _ = true, aggregateId);
            var routedEvent = new RoutableTestEvent { Data = "Buffered", AggregateId = aggregateId };
            EventBus.Send(routedEvent); // This will be buffered

            // Act
            EventBus.ClearEventsForAggregate(aggregateId);

            // Assert
            Assert.AreEqual(0, EventBus.GetBufferedEventCount(aggregateId));
            Assert.IsTrue(EventBus.HasHandlersForAggregate(aggregateId));
            Assert.AreEqual(1, EventBus.GetHandlerCountForAggregate(aggregateId));
        }

        [Test]
        public void ClearEventsForAggregate_WithEmptyGuid_ClearsEventsButKeepsHandlers()
        {
            // Arrange
            EventBus.RegisterHandler<TestEvent>(evt => { }, Guid.Empty);
            var routedEvent = new RoutableTestEvent { Data = "Test", AggregateId = Guid.Empty };
            EventBus.Send(routedEvent);

            // Act
            EventBus.ClearEventsForAggregate(Guid.Empty);

            // Assert
            // ClearEventsForAggregate should clear events but keep handlers
            Assert.AreEqual(0, EventBus.GetQueueCount());
            Assert.AreEqual(1, EventBus.GetHandlerCountForAggregate(Guid.Empty));
        }

        [Test]
        public void ClearEventsForAggregate_WithNonExistentAggregate_DoesNotThrow()
        {
            // Arrange
            var nonExistentAggregateId = Guid.NewGuid();

            // Act & Assert
            Assert.DoesNotThrow(() => EventBus.ClearEventsForAggregate(nonExistentAggregateId));
        }

        [Test]
        public void ClearEventsForAggregate_WithNoEvents_DoesNotThrow()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            EventBus.RegisterHandler<TestEvent>(evt => { }, aggregateId);

            // Act & Assert
            Assert.DoesNotThrow(() => EventBus.ClearEventsForAggregate(aggregateId));
        }

        [Test]
        public void ClearEventsForAggregate_WithMixedEvents_ClearsAllForAggregate()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            EventBus.RegisterHandler<TestEvent>(evt => { }, aggregateId);
            EventBus.RegisterHandler<AnotherTestEvent>(evt => { }, aggregateId);
            
            // Send queued events
            EventBus.Send(new RoutableTestEvent { Data = "Test1", AggregateId = aggregateId });
            EventBus.Send(new RoutableTestEvent { Data = "Test2", AggregateId = aggregateId });
            
            // Send buffered event
            EventBus.Send(new RoutableTestEvent { Data = "Buffered", AggregateId = aggregateId });

            // Act
            EventBus.ClearEventsForAggregate(aggregateId);

            // Assert
            Assert.AreEqual(0, EventBus.GetQueueCount());
            Assert.AreEqual(0, EventBus.GetBufferedEventCount(aggregateId));
            Assert.AreEqual(2, EventBus.GetHandlerCountForAggregate(aggregateId));
        }
    }
}