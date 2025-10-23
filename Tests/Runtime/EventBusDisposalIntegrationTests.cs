using System;
using System.Threading.Tasks;
using com.DvosTools.bus.Dispatchers;
using NUnit.Framework;

namespace com.DvosTools.bus
{
    [TestFixture]
    public class EventBusDisposalIntegrationTests
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
        public void MultipleDisposalMethods_WorkTogether()
        {
            // Arrange
            var aggregateId1 = Guid.NewGuid();
            var aggregateId2 = Guid.NewGuid();
            var handler1Called = false;
            var handler2Called = false;
            var handler3Called = false;
            
            EventBus.RegisterHandler<TestEvent>(evt => handler1Called = true, aggregateId1);
            EventBus.RegisterHandler<TestEvent>(evt => handler2Called = true, aggregateId2);
            EventBus.RegisterHandler<AnotherTestEvent>(evt => handler3Called = true);

            // Act
            EventBus.ResetAggregate(aggregateId1);
            EventBus.DisposeHandlers<TestEvent>();
            EventBus.DisposeHandlersForAggregate<AnotherTestEvent>(aggregateId2);

            // Assert
            Assert.IsFalse(EventBus.HasHandlers<TestEvent>());
            Assert.IsTrue(EventBus.HasHandlers<AnotherTestEvent>());
            Assert.IsFalse(EventBus.HasHandlersForAggregate(aggregateId1));
            Assert.IsFalse(EventBus.HasHandlersForAggregate(aggregateId2));
        }

        [Test]
        public async Task DisposalMethods_AreThreadSafe()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            EventBus.RegisterHandler<TestEvent>(evt => { }, aggregateId);

            // Act - Run multiple disposal operations concurrently
            var tasks = new[]
            {
                Task.Run(() => EventBus.ResetAggregate(aggregateId)),
                Task.Run(() => EventBus.ClearEventsForAggregate(aggregateId)),
                Task.Run(() => EventBus.DisposeAllHandlers())
            };

            await Task.WhenAll(tasks);

            // Assert - Should not throw exceptions and system should be in clean state
            Assert.AreEqual(0, EventBus.GetHandlerCount<TestEvent>());
            Assert.AreEqual(0, EventBus.GetQueueCount());
        }

        [Test]
        public void DisposalMethods_WithComplexScenario_WorkCorrectly()
        {
            // Arrange - Create a complex scenario with multiple aggregates and event types
            var aggregateId1 = Guid.NewGuid();
            var aggregateId2 = Guid.NewGuid();
            var globalHandlerCalled = false;
            var aggregate1HandlerCalled = false;
            var aggregate2HandlerCalled = false;
            
            // Register handlers
            EventBus.RegisterHandler<TestEvent>(evt => globalHandlerCalled = true);
            EventBus.RegisterHandler<TestEvent>(evt => aggregate1HandlerCalled = true, aggregateId1);
            EventBus.RegisterHandler<AnotherTestEvent>(evt => aggregate2HandlerCalled = true, aggregateId2);
            
            // Lock the queue to prevent processing while we set up the test
            lock (EventBus.Instance.QueueLock)
            {
                // Send events while holding the lock - they'll be queued but not processed
                EventBus.Send(new TestEvent { Message = "Global" });
                EventBus.Send(new RoutableTestEvent { Data = "Aggregate1", AggregateId = aggregateId1 });
                EventBus.Send(new RoutableTestEvent { Data = "Aggregate2", AggregateId = aggregateId2 });
                
                // Verify we have 1 event in queue (the global TestEvent)
                Assert.AreEqual(1, EventBus.GetQueueCount());
                
                // Act - Perform various disposal operations
                EventBus.DisposeHandlersForAggregate<TestEvent>(aggregateId1);
                EventBus.ResetAggregate(aggregateId2);
                EventBus.ClearEventsForAggregate(aggregateId1);
                
                // Assert - Only global event should remain in queue
                Assert.AreEqual(1, EventBus.GetQueueCount()); // Only global event should remain
            }
            
            // Verify handlers were disposed correctly
            Assert.IsTrue(EventBus.HasHandlers<TestEvent>()); // Global handler should remain
            Assert.IsFalse(EventBus.HasHandlers<AnotherTestEvent>()); // Aggregate2 was reset
            Assert.IsFalse(EventBus.HasHandlersForAggregate(aggregateId1)); // Disposed
            Assert.IsFalse(EventBus.HasHandlersForAggregate(aggregateId2)); // Reset
        }

        [Test]
        public void DisposalMethods_WithBufferedEvents_HandleCorrectly()
        {
            // Arrange
            var aggregateId1 = Guid.NewGuid();
            var aggregateId2 = Guid.NewGuid();
            
            // Send events that will be buffered (no handlers yet)
            EventBus.Send(new RoutableTestEvent { Data = "Buffered1", AggregateId = aggregateId1 });
            EventBus.Send(new RoutableTestEvent { Data = "Buffered2", AggregateId = aggregateId2 });
            
            // Register handlers
            EventBus.RegisterHandler<TestEvent>(evt => { }, aggregateId1);
            EventBus.RegisterHandler<TestEvent>(evt => { }, aggregateId2);

            // Act
            EventBus.ResetAggregate(aggregateId1);
            EventBus.ClearEventsForAggregate(aggregateId2);

            // Assert
            Assert.AreEqual(0, EventBus.GetBufferedEventCount(aggregateId1)); // Reset cleared buffered events
            Assert.AreEqual(0, EventBus.GetBufferedEventCount(aggregateId2)); // ClearEvents cleared buffered events
            Assert.IsFalse(EventBus.HasHandlersForAggregate(aggregateId1)); // Reset removed handlers
            Assert.IsTrue(EventBus.HasHandlersForAggregate(aggregateId2)); // ClearEvents kept handlers
        }
    }
}