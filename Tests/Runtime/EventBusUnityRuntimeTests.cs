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
        private const int TimeoutMs = 1000; // 1 second

        [SetUp]
        public void SetUp()
        {
            // Clear all handlers and buffered events before each test
            EventBus.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            // Clear all handlers and buffered events after each test
            EventBus.ClearAll();
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
            Assert.IsTrue(EventBus.HasHandlers<TestEvent>());
            Assert.AreEqual(1, EventBus.GetHandlerCount<TestEvent>());
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
            EventBus.Send(testEvent);
            
            // Wait for Unity's Update() to process the queue
            yield return new WaitForSeconds(0.01f);
            
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
            Assert.IsTrue(EventBus.HasHandlers<TestEvent>());
            Assert.AreEqual(1, EventBus.GetHandlerCount<TestEvent>());
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
            EventBus.Send(testEvent);

            // Wait for Unity's Update() to process the queue
            yield return new WaitForSeconds(0.01f);

            // Assert with timeout handling
            Await.AtMost(TimeoutMs, () =>
            {
                Assert.IsTrue(EventBus.HasHandlers<TestEvent>());
                Assert.AreEqual(2, EventBus.GetHandlerCount<TestEvent>());
            });
        }

        [UnityTest]
        public IEnumerator Send_100EventsWithUnityDispatcher_AllProcessed()
        {
            // Arrange
            var unityDispatcher = UnityDispatcher.Instance;
            var aggregateId = Guid.NewGuid();
            var processedEvents = new List<int>();
            var expectedEvents = new HashSet<int>();

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
                expectedEvents.Add(i);
                EventBus.Send(new RoutableTestEvent 
                { 
                    AggregateId = aggregateId, 
                    Data = i.ToString() 
                });
            }

            // Wait for Unity's Update() to process the queue (may need multiple frames for 100 events)
            yield return new WaitForSeconds(0.1f);

            // Assert - All events should be processed (order may vary due to async batching)
            Await.AtMost(TimeoutMs, () =>
            {
                Assert.AreEqual(100, processedEvents.Count, "Should have processed all 100 events");
                
                // Verify all expected events were processed (order doesn't matter)
                var processedSet = new HashSet<int>(processedEvents);
                Assert.IsTrue(expectedEvents.SetEquals(processedSet), "All expected events should have been processed");
                
                Assert.AreEqual(0, EventBus.GetBufferedEventCount(aggregateId), "No events should be buffered");
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