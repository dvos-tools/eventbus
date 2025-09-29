using System;
using NUnit.Framework;
using com.DvosTools.bus.Runtime;
using com.DvosTools.bus.Runtime.Dispatchers;

namespace com.DvosTools.bus.Tests
{
    [TestFixture]
    public class EventBusAggregateIdTests
    {
        [SetUp]
        public void SetUp()
        {
            // Clear all handlers before each test
            EventBus.Instance.Handlers.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            // Clear all handlers after each test
            EventBus.Instance.Handlers.Clear();
        }

        [Test]
        public void RegisterHandler_WithAggregateId_HandlerIsRegisteredWithAggregateId()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var handlerCalled = false;

            // Act
            EventBus.RegisterHandler<TestEvent>(evt =>
            {
                handlerCalled = true;
            }, aggregateId);

            // Assert
            Assert.IsTrue(EventBus.Instance.Handlers.ContainsKey(typeof(TestEvent)));
            Assert.AreEqual(1, EventBus.Instance.Handlers[typeof(TestEvent)].Count);
            
            var subscription = EventBus.Instance.Handlers[typeof(TestEvent)][0];
            Assert.AreEqual(aggregateId, subscription.AggregateId);
            Assert.IsInstanceOf<ThreadPoolDispatcher>(subscription.Dispatcher);
        }

        [Test]
        public void RegisterHandler_MixedAggregateIds_SameEventType_AllHandlersRegistered()
        {
            // Arrange
            var aggregateId1 = Guid.NewGuid();
            var aggregateId2 = Guid.NewGuid();
            var handler1Called = false;
            var handler2Called = false;

            // Act
            EventBus.RegisterHandler<TestEvent>(evt => handler1Called = true, aggregateId1);
            EventBus.RegisterHandler<TestEvent>(evt => handler2Called = true, aggregateId2);

            // Assert
            Assert.IsTrue(EventBus.Instance.Handlers.ContainsKey(typeof(TestEvent)));
            Assert.AreEqual(2, EventBus.Instance.Handlers[typeof(TestEvent)].Count);
            
            var subscriptions = EventBus.Instance.Handlers[typeof(TestEvent)];
            Assert.Contains(aggregateId1, new[] { subscriptions[0].AggregateId, subscriptions[1].AggregateId });
            Assert.Contains(aggregateId2, new[] { subscriptions[0].AggregateId, subscriptions[1].AggregateId });
        }

        [Test]
        public void RegisterHandler_RegularAndAggregateIdHandlers_BothRegistered()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var regularHandlerCalled = false;
            var aggregateHandlerCalled = false;

            // Act
            EventBus.RegisterHandler<TestEvent>(evt => regularHandlerCalled = true);
            EventBus.RegisterHandler<TestEvent>(evt => aggregateHandlerCalled = true, aggregateId);

            // Assert
            Assert.IsTrue(EventBus.Instance.Handlers.ContainsKey(typeof(TestEvent)));
            Assert.AreEqual(2, EventBus.Instance.Handlers[typeof(TestEvent)].Count);
            
            var subscriptions = EventBus.Instance.Handlers[typeof(TestEvent)];
            Assert.Contains(Guid.Empty, new[] { subscriptions[0].AggregateId, subscriptions[1].AggregateId });
            Assert.Contains(aggregateId, new[] { subscriptions[0].AggregateId, subscriptions[1].AggregateId });
        }

        [Test]
        public void RegisterHandler_SameAggregateIdMultipleTimes_MultipleHandlersRegistered()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var handler1Called = false;
            var handler2Called = false;

            // Act
            EventBus.RegisterHandler<TestEvent>(evt => handler1Called = true, aggregateId);
            EventBus.RegisterHandler<TestEvent>(evt => handler2Called = true, aggregateId);

            // Assert
            Assert.IsTrue(EventBus.Instance.Handlers.ContainsKey(typeof(TestEvent)));
            Assert.AreEqual(2, EventBus.Instance.Handlers[typeof(TestEvent)].Count);
            
            var subscriptions = EventBus.Instance.Handlers[typeof(TestEvent)];
            Assert.AreEqual(aggregateId, subscriptions[0].AggregateId);
            Assert.AreEqual(aggregateId, subscriptions[1].AggregateId);
        }

        [Test]
        public void RegisterHandler_EmptyGuidAggregateId_TreatedAsRegularHandler()
        {
            // Arrange
            var handlerCalled = false;

            // Act
            EventBus.RegisterHandler<TestEvent>(evt =>
            {
                handlerCalled = true;
            }, Guid.Empty);

            // Assert
            Assert.IsTrue(EventBus.Instance.Handlers.ContainsKey(typeof(TestEvent)));
            Assert.AreEqual(1, EventBus.Instance.Handlers[typeof(TestEvent)].Count);
            
            var subscription = EventBus.Instance.Handlers[typeof(TestEvent)][0];
            Assert.AreEqual(Guid.Empty, subscription.AggregateId);
        }

        [Test]
        public void RegisterHandler_AggregateIdWithCustomDispatcher_HandlerRegisteredCorrectly()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var customDispatcher = new ImmediateDispatcher();
            var handlerCalled = false;

            // Act
            EventBus.RegisterHandler<TestEvent>(evt =>
            {
                handlerCalled = true;
            }, aggregateId, customDispatcher);

            // Assert
            Assert.IsTrue(EventBus.Instance.Handlers.ContainsKey(typeof(TestEvent)));
            Assert.AreEqual(1, EventBus.Instance.Handlers[typeof(TestEvent)].Count);
            
            var subscription = EventBus.Instance.Handlers[typeof(TestEvent)][0];
            Assert.AreEqual(aggregateId, subscription.AggregateId);
            Assert.AreEqual(customDispatcher, subscription.Dispatcher);
        }
    }
}