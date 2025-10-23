using System;
using com.DvosTools.bus.Dispatchers;
using NUnit.Framework;

namespace com.DvosTools.bus
{
    [TestFixture]
    public class EventBusDisposeHandlerFromAggregateTests
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
        public void DisposeHandlerFromAggregate_RemovesSpecificHandler()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var handler1Called = false;
            var handler2Called = false;
            Action<RoutableTestEvent> handler1 = evt => handler1Called = true;
            Action<RoutableTestEvent> handler2 = evt => handler2Called = true;
            
            EventBus.RegisterHandler(handler1, aggregateId);
            EventBus.RegisterHandler(handler2, aggregateId);

            // Act
            EventBus.DisposeHandlerFromAggregate(handler1, aggregateId);

            // Assert
            Assert.AreEqual(1, EventBus.GetHandlerCountForAggregate(aggregateId));
            EventBus.Send(new RoutableTestEvent { Data = "Test", AggregateId = aggregateId });
            
            // Wait a bit for async processing
            System.Threading.Thread.Sleep(100);
            
            Assert.IsFalse(handler1Called);
            Assert.IsTrue(handler2Called);
        }

        [Test]
        public void DisposeHandlerFromAggregate_ClearsEventsForAggregate()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            Action<TestEvent> handler = evt => { };
            EventBus.RegisterHandler(handler, aggregateId);
            
            var routedEvent = new RoutableTestEvent { Data = "Test", AggregateId = aggregateId };
            EventBus.Send(routedEvent);

            // Act
            EventBus.DisposeHandlerFromAggregate(handler, aggregateId);

            // Assert
            Assert.AreEqual(0, EventBus.GetQueueCount());
            Assert.AreEqual(0, EventBus.GetBufferedEventCount(aggregateId));
        }

        [Test]
        public void DisposeHandlerFromAggregate_WithNonExistentHandler_DoesNotThrow()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            Action<TestEvent> nonExistentHandler = evt => { };
            EventBus.RegisterHandler<TestEvent>(evt => { }, aggregateId);

            // Act & Assert
            Assert.DoesNotThrow(() => EventBus.DisposeHandlerFromAggregate(nonExistentHandler, aggregateId));
        }

        [Test]
        public void DisposeHandlerFromAggregate_WithNonExistentAggregate_DoesNotThrow()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var nonExistentAggregateId = Guid.NewGuid();
            Action<TestEvent> handler = evt => { };
            EventBus.RegisterHandler(handler, aggregateId);

            // Act & Assert
            Assert.DoesNotThrow(() => EventBus.DisposeHandlerFromAggregate(handler, nonExistentAggregateId));
        }

        [Test]
        public void DisposeHandlerFromAggregate_WithEmptyGuid_DoesNothing()
        {
            // Arrange
            Action<TestEvent> handler = evt => { };
            EventBus.RegisterHandler(handler, Guid.Empty);

            // Act
            EventBus.DisposeHandlerFromAggregate(handler, Guid.Empty);

            // Assert
            Assert.AreEqual(1, EventBus.GetHandlerCountForAggregate(Guid.Empty));
        }

        [Test]
        public void DisposeHandlerFromAggregate_WithNullHandler_DoesNotThrow()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();

            // Act & Assert
            Assert.DoesNotThrow(() => EventBus.DisposeHandlerFromAggregate<TestEvent>(null, aggregateId));
        }
    }
}