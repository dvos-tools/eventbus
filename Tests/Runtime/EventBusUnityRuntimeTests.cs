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

            // Wait for async processing with multiple yield points
            var maxWaitTime = 1.0f; // 1 second max
            var waitInterval = 0.05f; // Check every 50ms
            var elapsedTime = 0f;
            
            while (elapsedTime < maxWaitTime)
            {
                yield return new WaitForSeconds(waitInterval);
                elapsedTime += waitInterval;
                
                // Check if handler executed
                if (handlerExecutedOnMainThread)
                {
                    break;
                }
            }
            
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

            // Wait for async processing
            yield return new WaitForSeconds(0.1f);

            // Assert - Handler registration is immediate, no need for timeout
            Assert.IsTrue(EventBus.HasHandlers<TestEvent>());
            Assert.AreEqual(2, EventBus.GetHandlerCount<TestEvent>());
        }

        [UnityTest]
        public IEnumerator Send_400EventsWithUnityDispatcher_AllProcessedInOrder()
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
                    lock (processedEvents) // Thread-safe access to the list
                    {
                        processedEvents.Add(eventNumber);
                    }
                }
            }, aggregateId, unityDispatcher);

            // Act - Send 400 events with small delays to avoid overwhelming the system
            for (int i = 1; i <= 400; i++)
            {
                expectedOrder.Add(i);
                EventBus.Send(new RoutableTestEvent 
                { 
                    AggregateId = aggregateId, 
                    Data = i.ToString() 
                });
                
                // Small delay to prevent overwhelming the system
                if (i % 10 == 0)
                {
                    yield return null; // Yield every 10 events
                }
            }

            // Wait for async processing with multiple yield points
            var maxWaitTime = 10.0f; // 5 seconds max
            var waitInterval = 0.1f; // Check every 100ms
            var elapsedTime = 0f;
            
            while (elapsedTime < maxWaitTime)
            {
                yield return new WaitForSeconds(waitInterval);
                elapsedTime += waitInterval;
                
                // Check if all events are processed (thread-safe)
                int count;
                lock (processedEvents)
                {
                    count = processedEvents.Count;
                }
                
                if (count >= 400)
                {
                    break;
                }
            }

            // Assert - All events should be processed in FIFO order
            lock (processedEvents)
            {
                Assert.AreEqual(400, processedEvents.Count, "Should have processed all 100 events");
                
                // Verify FIFO order
                for (int i = 0; i < 400; i++)
                {
                    Assert.AreEqual(i + 1, processedEvents[i], $"Event {i + 1} should be processed in position {i}");
                }
            }
            
            Assert.AreEqual(0, EventBus.GetBufferedEventCount(aggregateId), "No events should be buffered");
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
                        // Use a small delay that doesn't block Unity's main thread
                        System.Threading.Tasks.Task.Delay(1).Wait();
                    }
                }

                // Timeout reached, throw the last exception
                throw new AssertionException($"Condition not met within {timeoutMs}ms. Last error: {lastException?.Message}", lastException);
            }
        }
    }
}