using System;
using System.Collections;
using com.DvosTools.bus.Runtime;
using com.DvosTools.bus.Runtime.Dispatchers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.DvosTools.bus
{
    [TestFixture]
    public class EventBusUnityRuntimeTests
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
        public void RegisterHandler_WithUnityDispatcher_HandlerUsesUnityDispatcher()
        {
            // Arrange
            var unityDispatcher = UnityDispatcher.Instance;

            // Act
            EventBus.RegisterHandler<TestEvent>(evt =>
            {
                _ = true;
            }, Guid.Empty, unityDispatcher);

            // Assert
            Assert.IsTrue(EventBus.Instance.Handlers.ContainsKey(typeof(TestEvent)));
            Assert.AreEqual(1, EventBus.Instance.Handlers[typeof(TestEvent)].Count);
            
            var subscription = EventBus.Instance.Handlers[typeof(TestEvent)][0];
            Assert.AreEqual(unityDispatcher, subscription.Dispatcher);
            Assert.AreEqual(Guid.Empty, subscription.AggregateId);
        }

        [UnityTest]
        public IEnumerator RegisterHandler_UnityDispatcher_ExecutesOnMainThread()
        {
            // Arrange
            var unityDispatcher = UnityDispatcher.Instance;
            var handlerExecutedOnMainThread = false;
            var originalThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            EventBus.RegisterHandler<TestEvent>(_ =>
            {
                handlerExecutedOnMainThread = System.Threading.Thread.CurrentThread.ManagedThreadId == originalThreadId;
            }, Guid.Empty, unityDispatcher);

            // Act
            var testEvent = new TestEvent { Message = "Unity Test" };
            EventBus.Instance.Send(testEvent);

            // Wait for async processing
            yield return new WaitForSeconds(0.1f);

            // Assert
            Assert.IsTrue(handlerExecutedOnMainThread, "Handler should execute on main thread when using UnityDispatcher");
        }

        [Test]
        public void RegisterHandler_UnityDispatcherWithAggregateId_HandlerRegisteredCorrectly()
        {
            // Arrange
            var unityDispatcher = UnityDispatcher.Instance;
            var aggregateId = Guid.NewGuid();

            // Act
            EventBus.RegisterHandler<TestEvent>(evt =>
            {
                _ = true;
            }, aggregateId, unityDispatcher);

            // Assert
            Assert.IsTrue(EventBus.Instance.Handlers.ContainsKey(typeof(TestEvent)));
            Assert.AreEqual(1, EventBus.Instance.Handlers[typeof(TestEvent)].Count);
            
            var subscription = EventBus.Instance.Handlers[typeof(TestEvent)][0];
            Assert.AreEqual(unityDispatcher, subscription.Dispatcher);
            Assert.AreEqual(aggregateId, subscription.AggregateId);
        }

        [UnityTest]
        public IEnumerator RegisterHandler_MultipleUnityDispatchers_AllRegisteredCorrectly()
        {
            // Arrange
            var unityDispatcher = UnityDispatcher.Instance;

            EventBus.RegisterHandler<TestEvent>(evt => _ = true, Guid.Empty, unityDispatcher);
            EventBus.RegisterHandler<TestEvent>(evt => _ = true, Guid.Empty, unityDispatcher);

            // Act
            var testEvent = new TestEvent { Message = "Multiple Unity Test" };
            EventBus.Instance.Send(testEvent);

            // Wait for async processing
            yield return new WaitForSeconds(0.1f);

            // Assert
            Assert.IsTrue(EventBus.Instance.Handlers.ContainsKey(typeof(TestEvent)));
            Assert.AreEqual(2, EventBus.Instance.Handlers[typeof(TestEvent)].Count);
            
            var subscriptions = EventBus.Instance.Handlers[typeof(TestEvent)];
            Assert.AreEqual(unityDispatcher, subscriptions[0].Dispatcher);
            Assert.AreEqual(unityDispatcher, subscriptions[1].Dispatcher);
        }
    }
}