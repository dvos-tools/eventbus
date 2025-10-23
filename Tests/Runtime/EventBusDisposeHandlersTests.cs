using System;
using com.DvosTools.bus.Dispatchers;
using NUnit.Framework;

namespace com.DvosTools.bus
{
    [TestFixture]
    public class EventBusDisposeHandlersTests
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
        public void DisposeHandlers_RemovesSpecificEventTypeHandlers()
        {
            // Arrange
            var testEventHandlerCalled = false;
            var anotherEventHandlerCalled = false;
            EventBus.RegisterHandler<TestEvent>(evt => testEventHandlerCalled = true);
            EventBus.RegisterHandler<AnotherTestEvent>(evt => anotherEventHandlerCalled = true);

            // Act
            EventBus.DisposeHandlers<TestEvent>();

            // Assert
            Assert.IsFalse(EventBus.HasHandlers<TestEvent>());
            Assert.IsTrue(EventBus.HasHandlers<AnotherTestEvent>());
            Assert.AreEqual(0, EventBus.GetHandlerCount<TestEvent>());
            Assert.AreEqual(1, EventBus.GetHandlerCount<AnotherTestEvent>());
        }

        [Test]
        public void DisposeHandlers_ClearsQueuedEventsForSpecificType()
        {
            // Arrange
            EventBus.RegisterHandler<TestEvent>(evt => { });
            EventBus.RegisterHandler<AnotherTestEvent>(evt => { });
            
            // Lock the queue to prevent processing while we set up the test
            lock (EventBus.Instance.QueueLock)
            {
                // Send events while holding the lock - they'll be queued but not processed
                EventBus.Send(new TestEvent { Message = "Test1" });
                EventBus.Send(new AnotherTestEvent { Description = "Another1" });
                EventBus.Send(new TestEvent { Message = "Test2" });
                
                // Verify we have 3 events in queue
                Assert.AreEqual(3, EventBus.GetQueueCount());
                
                // Act - Dispose TestEvent handlers
                EventBus.DisposeHandlers<TestEvent>();
                
                // Assert - Only AnotherTestEvent should remain in queue
                Assert.AreEqual(1, EventBus.GetQueueCount());
            }
            
            // Verify handlers were removed correctly
            Assert.IsFalse(EventBus.HasHandlers<TestEvent>());
            Assert.IsTrue(EventBus.HasHandlers<AnotherTestEvent>());
            Assert.AreEqual(0, EventBus.GetHandlerCount<TestEvent>());
            Assert.AreEqual(1, EventBus.GetHandlerCount<AnotherTestEvent>());
        }

        [Test]
        public void DisposeHandlers_DoesNotAffectBufferedEvents()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var routedEvent = new RoutableTestEvent { Data = "Buffered", AggregateId = aggregateId };
            EventBus.Send(routedEvent); // This will be buffered

            // Act
            EventBus.DisposeHandlers<TestEvent>();

            // Assert
            Assert.AreEqual(1, EventBus.GetTotalBufferedEventCount());
            Assert.IsTrue(EventBus.HasBufferedEvents());
        }

        [Test]
        public void DisposeHandlers_WithMultipleHandlers_RemovesAllOfType()
        {
            // Arrange
            var handler1Called = false;
            var handler2Called = false;
            var handler3Called = false;
            EventBus.RegisterHandler<TestEvent>(evt => handler1Called = true);
            EventBus.RegisterHandler<TestEvent>(evt => handler2Called = true);
            EventBus.RegisterHandler<AnotherTestEvent>(evt => handler3Called = true);

            // Act
            EventBus.DisposeHandlers<TestEvent>();

            // Assert
            Assert.IsFalse(EventBus.HasHandlers<TestEvent>());
            Assert.IsTrue(EventBus.HasHandlers<AnotherTestEvent>());
            Assert.AreEqual(0, EventBus.GetHandlerCount<TestEvent>());
            Assert.AreEqual(1, EventBus.GetHandlerCount<AnotherTestEvent>());
        }

        [Test]
        public void DisposeHandlers_WithNoHandlers_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => EventBus.DisposeHandlers<TestEvent>());
        }
    }
}