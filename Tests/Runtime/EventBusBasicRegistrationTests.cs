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
            EventBus.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            // Clear all handlers after each test
            EventBus.ClearAll();
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
            Assert.IsTrue(EventBus.HasHandlers<TestEvent>());
            Assert.AreEqual(1, EventBus.GetHandlerCount<TestEvent>());
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
            Assert.IsTrue(EventBus.HasHandlers<TestEvent>());
            Assert.AreEqual(2, EventBus.GetHandlerCount<TestEvent>());
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
            Assert.IsTrue(EventBus.HasHandlers<TestEvent>());
            Assert.AreEqual(1, EventBus.GetHandlerCount<TestEvent>());
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
            Assert.IsTrue(EventBus.HasHandlers<TestEvent>());
            Assert.IsTrue(EventBus.HasHandlers<AnotherTestEvent>());
            Assert.AreEqual(1, EventBus.GetHandlerCount<TestEvent>());
            Assert.AreEqual(1, EventBus.GetHandlerCount<AnotherTestEvent>());
        }
    }
}