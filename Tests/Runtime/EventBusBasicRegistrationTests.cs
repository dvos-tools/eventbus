using System;
using com.DvosTools.bus.Dispatchers;
using NUnit.Framework;

namespace com.DvosTools.bus
{
    [TestFixture]
    public class EventBusBasicRegistrationTests
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
        public void RegisterHandler_BasicRegistration_HandlerIsRegistered()
        {
            // Arrange
            var testEvent = new TestEvent { Message = "Hello World" };
            var handlerCalled = false;
            TestEvent receivedEvent = null;

            // Act
            EventBus.RegisterHandler<TestEvent>(evt =>
            {
                handlerCalled = true;
                receivedEvent = evt;
            });

            // Assert
            Assert.IsTrue(EventBus.Instance.Handlers.ContainsKey(typeof(TestEvent)));
            Assert.AreEqual(1, EventBus.Instance.Handlers[typeof(TestEvent)].Count);
            
            var subscription = EventBus.Instance.Handlers[typeof(TestEvent)][0];
            Assert.AreEqual(Guid.Empty, subscription.AggregateId);
            Assert.IsInstanceOf<ThreadPoolDispatcher>(subscription.Dispatcher);
        }

        [Test]
        public void RegisterHandler_MultipleHandlers_SameEventType_AllHandlersRegistered()
        {
            // Arrange
            var handler1Called = false;
            var handler2Called = false;

            // Act
            EventBus.RegisterHandler<TestEvent>(evt => handler1Called = true);
            EventBus.RegisterHandler<TestEvent>(evt => handler2Called = true);

            // Assert
            Assert.IsTrue(EventBus.Instance.Handlers.ContainsKey(typeof(TestEvent)));
            Assert.AreEqual(2, EventBus.Instance.Handlers[typeof(TestEvent)].Count);
        }

        [Test]
        public void RegisterHandler_WithCustomDispatcher_HandlerUsesCustomDispatcher()
        {
            // Arrange
            var customDispatcher = new ImmediateDispatcher();
            var handlerCalled = false;

            // Act
            EventBus.RegisterHandler<TestEvent>(evt =>
            {
                handlerCalled = true;
            }, Guid.Empty, customDispatcher);

            // Assert
            Assert.IsTrue(EventBus.Instance.Handlers.ContainsKey(typeof(TestEvent)));
            Assert.AreEqual(1, EventBus.Instance.Handlers[typeof(TestEvent)].Count);
            
            var subscription = EventBus.Instance.Handlers[typeof(TestEvent)][0];
            Assert.AreEqual(customDispatcher, subscription.Dispatcher);
            Assert.AreEqual(Guid.Empty, subscription.AggregateId);
        }

        [Test]
        public void RegisterHandler_DifferentEventTypes_SeparateHandlersRegistered()
        {
            // Arrange
            var testEvent1HandlerCalled = false;
            var testEvent2HandlerCalled = false;

            // Act
            EventBus.RegisterHandler<TestEvent>(evt => testEvent1HandlerCalled = true);
            EventBus.RegisterHandler<AnotherTestEvent>(evt => testEvent2HandlerCalled = true);

            // Assert
            Assert.IsTrue(EventBus.Instance.Handlers.ContainsKey(typeof(TestEvent)));
            Assert.IsTrue(EventBus.Instance.Handlers.ContainsKey(typeof(AnotherTestEvent)));
            Assert.AreEqual(1, EventBus.Instance.Handlers[typeof(TestEvent)].Count);
            Assert.AreEqual(1, EventBus.Instance.Handlers[typeof(AnotherTestEvent)].Count);
        }
    }
}