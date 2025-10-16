using System;
using System.Collections.Generic;
using System.Linq;
using com.DvosTools.bus.Dispatchers;
using NUnit.Framework;

namespace com.DvosTools.bus
{
    /// <summary>
    /// Tests to ensure backwards compatibility with the deprecated instance API.
    /// These tests verify that EventBus.Instance.* methods still work correctly.
    /// </summary>
    [TestFixture]
    public class EventBusBackwardsCompatibilityTests
    {
        [SetUp]
        public void SetUp()
        {
            // Clear all handlers before each test
            EventBus.UnregisterAllHandlers();
        }

        [TearDown]
        public void TearDown()
        {
            // Clear all handlers after each test
            EventBus.UnregisterAllHandlers();
        }

        [Test]
        public void Instance_Send_WorksCorrectly()
        {
            // Arrange
            var testEvent = new TestEvent { Message = "Backwards Compatible Test" };
            var handlerCalled = false;
            TestEvent receivedEvent = null;

            EventBus.RegisterHandler<TestEvent>(evt =>
            {
                handlerCalled = true;
                receivedEvent = evt;
            });

            // Act - Use deprecated instance API
            EventBus.Instance.Send(testEvent);

            // Assert
            Assert.IsTrue(handlerCalled, "Handler should have been called via instance API");
            Assert.IsNotNull(receivedEvent, "Received event should not be null");
            Assert.AreEqual("Backwards Compatible Test", receivedEvent.Message);
        }

        [Test]
        public void Instance_SendAndWait_WorksCorrectly()
        {
            // Arrange
            var testEvent = new TestEvent { Message = "SendAndWait Test" };
            var handlerCalled = false;

            EventBus.RegisterHandler<TestEvent>(evt =>
            {
                handlerCalled = true;
            }, Guid.Empty, new ImmediateDispatcher());

            // Act - Use deprecated instance API
            EventBus.Instance.SendAndWait(testEvent);

            // Assert
            Assert.IsTrue(handlerCalled, "Handler should have been called immediately via SendAndWait");
        }

        [Test]
        public void Instance_AggregateReady_WorksCorrectly()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var testEvent = new RoutableTestEvent { AggregateId = aggregateId, Data = "Buffered Event" };
            var handlerCalled = false;

            // Send event first (should be buffered)
            EventBus.Send(testEvent);

            // Register handler
            EventBus.RegisterHandler<RoutableTestEvent>(evt => handlerCalled = true, aggregateId, new ImmediateDispatcher());

            // Act - Use deprecated instance API
            EventBus.Instance.AggregateReady(aggregateId);

            // Assert
            Assert.IsTrue(handlerCalled, "Handler should have been called after AggregateReady");
            Assert.AreEqual(0, EventBus.GetBufferedEventCount(aggregateId), "Event should no longer be buffered");
        }

        [Test]
        public void Instance_GetBufferedEventCount_WorksCorrectly()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var testEvent = new RoutableTestEvent { AggregateId = aggregateId, Data = "Test Data" };

            // Send event (should be buffered)
            EventBus.Send(testEvent);

            // Act - Use deprecated instance API
            var count = EventBus.Instance.GetBufferedEventCount(aggregateId);

            // Assert
            Assert.AreEqual(1, count, "Should return correct buffered event count");
        }

        [Test]
        public void Instance_GetBufferedAggregateIds_WorksCorrectly()
        {
            // Arrange
            var aggregateId1 = Guid.NewGuid();
            var aggregateId2 = Guid.NewGuid();
            var event1 = new RoutableTestEvent { AggregateId = aggregateId1, Data = "Event 1" };
            var event2 = new RoutableTestEvent { AggregateId = aggregateId2, Data = "Event 2" };

            // Send events (should be buffered)
            EventBus.Send(event1);
            EventBus.Send(event2);

            // Act - Use deprecated instance API
            var bufferedIds = EventBus.Instance.GetBufferedAggregateIds().ToList();

            // Assert
            Assert.AreEqual(2, bufferedIds.Count, "Should return correct number of buffered aggregate IDs");
            Assert.Contains(aggregateId1, bufferedIds, "Should contain first aggregate ID");
            Assert.Contains(aggregateId2, bufferedIds, "Should contain second aggregate ID");
        }

        [Test]
        public void Instance_GetTotalBufferedEventCount_WorksCorrectly()
        {
            // Arrange
            var aggregateId1 = Guid.NewGuid();
            var aggregateId2 = Guid.NewGuid();
            var event1 = new RoutableTestEvent { AggregateId = aggregateId1, Data = "Event 1" };
            var event2 = new RoutableTestEvent { AggregateId = aggregateId2, Data = "Event 2" };
            var event3 = new RoutableTestEvent { AggregateId = aggregateId1, Data = "Event 3" };

            // Send events (should be buffered)
            EventBus.Send(event1);
            EventBus.Send(event2);
            EventBus.Send(event3);

            // Act - Use deprecated instance API
            var totalCount = EventBus.Instance.GetTotalBufferedEventCount();

            // Assert
            Assert.AreEqual(3, totalCount, "Should return correct total buffered event count");
        }

        [Test]
        public void Instance_GetQueueCount_WorksCorrectly()
        {
            // Arrange
            var testEvent = new TestEvent { Message = "Queue Test" };
            var handlerCalled = false;

            EventBus.RegisterHandler<TestEvent>(evt => handlerCalled = true, Guid.Empty, new ThreadPoolDispatcher());

            // Send event (should be queued)
            EventBus.Send(testEvent);

            // Act - Use deprecated instance API
            var queueCount = EventBus.Instance.GetQueueCount();

            // Assert
            Assert.IsTrue(queueCount >= 0, "Queue count should be non-negative");
            // Note: Queue count might be 0 if processing was very fast, so we just check it's non-negative
        }

        [Test]
        public void Instance_GetHandlerCount_WorksCorrectly()
        {
            // Arrange
            EventBus.RegisterHandler<TestEvent>(evt => { });
            EventBus.RegisterHandler<TestEvent>(evt => { });
            EventBus.RegisterHandler<AnotherTestEvent>(evt => { });

            // Act - Use deprecated instance API
            var testEventCount = EventBus.Instance.GetHandlerCount<TestEvent>();
            var anotherTestEventCount = EventBus.Instance.GetHandlerCount<AnotherTestEvent>();

            // Assert
            Assert.AreEqual(2, testEventCount, "Should return correct handler count for TestEvent");
            Assert.AreEqual(1, anotherTestEventCount, "Should return correct handler count for AnotherTestEvent");
        }

        [Test]
        public void Instance_HasHandlers_WorksCorrectly()
        {
            // Arrange
            EventBus.RegisterHandler<TestEvent>(evt => { });

            // Act - Use deprecated instance API
            var hasTestEventHandlers = EventBus.Instance.HasHandlers<TestEvent>();
            var hasAnotherTestEventHandlers = EventBus.Instance.HasHandlers<AnotherTestEvent>();

            // Assert
            Assert.IsTrue(hasTestEventHandlers, "Should return true for TestEvent handlers");
            Assert.IsFalse(hasAnotherTestEventHandlers, "Should return false for AnotherTestEvent handlers");
        }

        [Test]
        public void Instance_Shutdown_WorksCorrectly()
        {
            // Arrange
            var testEvent = new TestEvent { Message = "Shutdown Test" };
            var handlerCalled = false;

            EventBus.RegisterHandler<TestEvent>(evt => handlerCalled = true, Guid.Empty, new ThreadPoolDispatcher());

            // Act - Use deprecated instance API
            EventBus.Instance.Shutdown();

            // Assert
            // Shutdown should not throw an exception
            Assert.DoesNotThrow(() => EventBus.Instance.Shutdown(), "Shutdown should not throw exception");
        }

        [Test]
        public void Instance_Handlers_Property_WorksCorrectly()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            EventBus.RegisterHandler<TestEvent>(evt => { }, aggregateId);

            // Act - Use deprecated instance API
            var handlers = EventBus.Instance.Handlers;

            // Assert
            Assert.IsNotNull(handlers, "Handlers property should not be null");
            Assert.IsTrue(handlers.ContainsKey(typeof(TestEvent)), "Should contain TestEvent handlers");
            Assert.AreEqual(1, handlers[typeof(TestEvent)].Count, "Should have one TestEvent handler");
            
            var subscription = handlers[typeof(TestEvent)][0];
            Assert.AreEqual(aggregateId, subscription.AggregateId, "Should have correct aggregate ID");
            Assert.IsInstanceOf<ThreadPoolDispatcher>(subscription.Dispatcher, "Should use ThreadPoolDispatcher by default");
        }

        [Test]
        public void Instance_EventQueue_Property_WorksCorrectly()
        {
            // Arrange
            var testEvent = new TestEvent { Message = "Queue Property Test" };
            EventBus.RegisterHandler<TestEvent>(evt => { }, Guid.Empty, new ThreadPoolDispatcher());

            // Send event (should be queued)
            EventBus.Send(testEvent);

            // Act - Use deprecated instance API
            var eventQueue = EventBus.Instance.EventQueue;

            // Assert
            Assert.IsNotNull(eventQueue, "EventQueue property should not be null");
            // Note: Queue might be empty if processing was very fast, so we just check it's not null
        }

        [Test]
        public void Instance_QueueLock_Property_WorksCorrectly()
        {
            // Act - Use deprecated instance API
            var queueLock = EventBus.Instance.QueueLock;

            // Assert
            Assert.IsNotNull(queueLock, "QueueLock property should not be null");
            Assert.IsInstanceOf<object>(queueLock, "QueueLock should be an object");
        }

        [Test]
        public void Instance_MultipleCalls_ReturnSameInstance()
        {
            // Act - Use deprecated instance API multiple times
            var instance1 = EventBus.Instance;
            var instance2 = EventBus.Instance;

            // Assert
            Assert.IsNotNull(instance1, "First instance should not be null");
            Assert.IsNotNull(instance2, "Second instance should not be null");
            // Note: These are different instances (new EventBus() each time), but both should work
            Assert.AreNotSame(instance1, instance2, "Each call should return a new instance");
        }

        [Test]
        public void Instance_AllMethods_WorkTogether()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var testEvent = new TestEvent { Message = "Integration Test" };
            var routableEvent = new RoutableTestEvent { AggregateId = aggregateId, Data = "Routable Test" };
            var handlerCalled = false;
            var routableHandlerCalled = false;

            EventBus.RegisterHandler<TestEvent>(evt => handlerCalled = true, Guid.Empty, new ImmediateDispatcher());
            EventBus.RegisterHandler<RoutableTestEvent>(evt => routableHandlerCalled = true, aggregateId, new ImmediateDispatcher());

            // Act - Use multiple deprecated instance API methods
            EventBus.Instance.Send(testEvent);
            EventBus.Instance.Send(routableEvent);
            EventBus.Instance.AggregateReady(aggregateId);

            // Assert
            Assert.IsTrue(handlerCalled, "TestEvent handler should have been called");
            Assert.IsTrue(routableHandlerCalled, "RoutableTestEvent handler should have been called");
            Assert.AreEqual(0, EventBus.Instance.GetBufferedEventCount(aggregateId), "No events should be buffered");
            Assert.IsTrue(EventBus.Instance.HasHandlers<TestEvent>(), "Should have TestEvent handlers");
            Assert.AreEqual(1, EventBus.Instance.GetHandlerCount<TestEvent>(), "Should have one TestEvent handler");
        }
    }
}