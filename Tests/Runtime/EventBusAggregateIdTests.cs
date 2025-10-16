using System;
using com.DvosTools.bus.Dispatchers;
using NUnit.Framework;

namespace com.DvosTools.bus
{
    [TestFixture]
    public class EventBusAggregateIdTests
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
            Assert.IsTrue(EventBus.HasHandlers<TestEvent>());
            Assert.AreEqual(1, EventBus.GetHandlerCount<TestEvent>());
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
            Assert.IsTrue(EventBus.HasHandlers<TestEvent>());
            Assert.AreEqual(2, EventBus.GetHandlerCount<TestEvent>());
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
            Assert.IsTrue(EventBus.HasHandlers<TestEvent>());
            Assert.AreEqual(2, EventBus.GetHandlerCount<TestEvent>());
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
            Assert.IsTrue(EventBus.HasHandlers<TestEvent>());
            Assert.AreEqual(2, EventBus.GetHandlerCount<TestEvent>());
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
            Assert.IsTrue(EventBus.HasHandlers<TestEvent>());
            Assert.AreEqual(1, EventBus.GetHandlerCount<TestEvent>());
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
            Assert.IsTrue(EventBus.HasHandlers<TestEvent>());
            Assert.AreEqual(1, EventBus.GetHandlerCount<TestEvent>());
        }
    }
}