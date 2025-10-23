using System;
using com.DvosTools.bus.Dispatchers;
using NUnit.Framework;

namespace com.DvosTools.bus
{
    [TestFixture]
    public class EventBusDisposeHandlersForAggregateTests
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
        public void DisposeHandlersForAggregate_RemovesSpecificEventTypeForAggregate()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var testEventHandlerCalled = false;
            var anotherEventHandlerCalled = false;
            EventBus.RegisterHandler<TestEvent>(evt => testEventHandlerCalled = true, aggregateId);
            EventBus.RegisterHandler<AnotherTestEvent>(evt => anotherEventHandlerCalled = true, aggregateId);

            // Act
            EventBus.DisposeHandlersForAggregate<TestEvent>(aggregateId);

            // Assert
            // Only TestEvent handlers should be removed, AnotherTestEvent handlers should remain
            Assert.AreEqual(1, EventBus.GetHandlerCountForAggregate(aggregateId));
            Assert.IsTrue(EventBus.HasHandlersForAggregate(aggregateId));
            Assert.IsFalse(EventBus.HasHandlers<TestEvent>());
            Assert.IsTrue(EventBus.HasHandlers<AnotherTestEvent>());
        }

        [Test]
        public void DisposeHandlersForAggregate_ClearsEventsForAggregate()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            EventBus.RegisterHandler<TestEvent>(evt => { }, aggregateId);
            var routedEvent = new RoutableTestEvent { Data = "Test", AggregateId = aggregateId };
            EventBus.Send(routedEvent);

            // Act
            EventBus.DisposeHandlersForAggregate<TestEvent>(aggregateId);

            // Assert
            Assert.AreEqual(0, EventBus.GetQueueCount());
            Assert.AreEqual(0, EventBus.GetBufferedEventCount(aggregateId));
        }

        [Test]
        public void DisposeHandlersForAggregate_WithMultipleHandlersOfSameType_RemovesAll()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var handler1Called = false;
            var handler2Called = false;
            EventBus.RegisterHandler<TestEvent>(evt => handler1Called = true, aggregateId);
            EventBus.RegisterHandler<TestEvent>(evt => handler2Called = true, aggregateId);

            // Act
            EventBus.DisposeHandlersForAggregate<TestEvent>(aggregateId);

            // Assert
            Assert.AreEqual(0, EventBus.GetHandlerCountForAggregate(aggregateId));
            Assert.IsFalse(EventBus.HasHandlersForAggregate(aggregateId));
        }

        [Test]
        public void DisposeHandlersForAggregate_WithEmptyGuid_DoesNothing()
        {
            // Arrange
            EventBus.RegisterHandler<TestEvent>(evt => { }, Guid.Empty);

            // Act
            EventBus.DisposeHandlersForAggregate<TestEvent>(Guid.Empty);

            // Assert
            Assert.AreEqual(1, EventBus.GetHandlerCountForAggregate(Guid.Empty));
        }

        [Test]
        public void DisposeHandlersForAggregate_WithNonExistentAggregate_DoesNotThrow()
        {
            // Arrange
            var nonExistentAggregateId = Guid.NewGuid();

            // Act & Assert
            Assert.DoesNotThrow(() => EventBus.DisposeHandlersForAggregate<TestEvent>(nonExistentAggregateId));
        }

        [Test]
        public void DisposeHandlersForAggregate_WithNoHandlersOfType_DoesNotThrow()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            EventBus.RegisterHandler<AnotherTestEvent>(evt => { }, aggregateId);

            // Act & Assert
            Assert.DoesNotThrow(() => EventBus.DisposeHandlersForAggregate<TestEvent>(aggregateId));
        }
    }
}