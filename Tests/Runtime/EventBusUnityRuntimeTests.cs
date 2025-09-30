using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using com.DvosTools.bus.Dispatchers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.DvosTools.bus
{
    [TestFixture]
    public class EventBusUnityRuntimeTests
    {
        private EventBus _eventBus;
        private const int TimeoutMs = 1000; // 1 second

        [SetUp]
        public void SetUp()
        {
            _eventBus = EventBus.Instance;
            // Clear all handlers and buffered events before each test
            _eventBus.Handlers.Clear();
            _eventBus.BufferedEvents.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            // Clear all handlers and buffered events after each test
            _eventBus.Handlers.Clear();
            _eventBus.BufferedEvents.Clear();
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
            var originalThreadId = Thread.CurrentThread.ManagedThreadId;

            EventBus.RegisterHandler<TestEvent>(_ =>
            {
                handlerExecutedOnMainThread = Thread.CurrentThread.ManagedThreadId == originalThreadId;
            }, Guid.Empty, unityDispatcher);

            // Act
            var testEvent = new TestEvent { Message = "Unity Test" };
            EventBus.Instance.Send(testEvent);

            // Wait for async processing with timeout
            yield return new WaitForSeconds(0.1f);
            
            // Assert with timeout handling
            Await.AtMost(TimeoutMs, () =>
            {
                Assert.IsTrue(handlerExecutedOnMainThread, "Handler should execute on main thread when using UnityDispatcher");
            });
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

            // Wait for async processing with timeout
            yield return new WaitForSeconds(0.1f);

            // Assert with timeout handling
            Await.AtMost(TimeoutMs, () =>
            {
                Assert.IsTrue(EventBus.Instance.Handlers.ContainsKey(typeof(TestEvent)));
                Assert.AreEqual(2, EventBus.Instance.Handlers[typeof(TestEvent)].Count);
                
                var subscriptions = EventBus.Instance.Handlers[typeof(TestEvent)];
                Assert.AreEqual(unityDispatcher, subscriptions[0].Dispatcher);
                Assert.AreEqual(unityDispatcher, subscriptions[1].Dispatcher);
            });
        }

        [UnityTest]
        public IEnumerator Send_100EventsWithUnityDispatcher_AllProcessedInOrder()
        {
            // Arrange
            var unityDispatcher = UnityDispatcher.Instance;
            var aggregateId = Guid.NewGuid();
            var processedEvents = new List<int>();
            var expectedOrder = new List<int>();
            if (expectedOrder == null) throw new ArgumentNullException(nameof(expectedOrder));

            // Register handler to collect processed events
            EventBus.RegisterHandler<RoutableTestEvent>(evt => 
            {
                if (int.TryParse(evt.Data, out var eventNumber))
                {
                    processedEvents.Add(eventNumber);
                }
            }, aggregateId, unityDispatcher);

            // Act - Send 100 events
            for (int i = 1; i <= 100; i++)
            {
                expectedOrder.Add(i);
                _eventBus.Send(new RoutableTestEvent 
                { 
                    AggregateId = aggregateId, 
                    Data = i.ToString() 
                });
            }

            // Wait for async processing
            yield return new WaitForSeconds(0.5f);

            // Assert - All events should be processed in FIFO order
            Await.AtMost(TimeoutMs, () =>
            {
                Assert.AreEqual(100, processedEvents.Count, "Should have processed all 100 events");
                
                // Verify FIFO order
                for (int i = 0; i < 100; i++)
                {
                    Assert.AreEqual(i + 1, processedEvents[i], $"Event {i + 1} should be processed in position {i}");
                }
                
                Assert.AreEqual(0, _eventBus.GetBufferedEventCount(aggregateId), "No events should be buffered");
            });
        }

        /// <summary>
        /// Helper class to wait for conditions with timeout
        /// </summary>
        private static class Await
        {
            public static void AtMost(int timeoutMs, Action assertions)
            {
                var startTime = DateTime.UtcNow;
                Exception lastException = null;

                while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
                {
                    try
                    {
                        assertions();
                        return; // Success!
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        Thread.Sleep(10); // Small delay before retry
                    }
                }

                // Timeout reached, throw the last exception
                throw new AssertionException($"Condition not met within {timeoutMs}ms. Last error: {lastException?.Message}", lastException);
            }
        }
    }
}