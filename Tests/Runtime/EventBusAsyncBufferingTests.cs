using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using com.DvosTools.bus.Dispatchers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.DvosTools.bus
{
    [TestFixture]
    public class EventBusAsyncBufferingTests
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
        public void Send_RoutableEventWithHandler_EventIsProcessedAsynchronously()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var testEvent = new RoutableTestEvent { AggregateId = aggregateId, Data = "Test Data" };
            var handlerCalled = false;
            var handlerCallCount = 0;

            EventBus.RegisterHandler<RoutableTestEvent>(_ => 
            {
                handlerCalled = true;
                handlerCallCount++;
            }, aggregateId, new ThreadPoolDispatcher());

            // Act
            EventBus.Send(testEvent);

            // Assert - Wait for async processing
            Await.AtMost(TimeoutMs, () =>
            {
                Assert.IsTrue(handlerCalled, "Handler should have been called");
                Assert.AreEqual(1, handlerCallCount, "Handler should have been called exactly once");
                Assert.AreEqual(0, EventBus.GetBufferedEventCount(aggregateId), "Event should not be buffered");
            });
        }


        [Test]
        public void MarkAggregateEventTypeReady_WithBufferedEvents_EventsAreProcessedAsynchronously()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var event1 = new RoutableTestEvent { AggregateId = aggregateId, Data = "Event 1" };
            var event2 = new RoutableTestEvent { AggregateId = aggregateId, Data = "Event 2" };
            var handlerCalledCount = 0;
            var receivedData = new List<string>();

            EventBus.Send(event1);
            EventBus.Send(event2);

            EventBus.RegisterHandler<RoutableTestEvent>(evt => 
            {
                handlerCalledCount++;
                receivedData.Add(evt.Data);
            }, aggregateId, new ThreadPoolDispatcher());

            // Act
            EventBus.AggregateReady(aggregateId);

            // Assert - Wait for async processing
            Await.AtMost(TimeoutMs, () =>
            {
                Assert.AreEqual(2, handlerCalledCount, "Handler should have been called twice");
                Assert.Contains("Event 1", receivedData, "Should have received Event 1");
                Assert.Contains("Event 2", receivedData, "Should have received Event 2");
                Assert.AreEqual(0, EventBus.GetBufferedEventCount(aggregateId), "No events should be buffered");
                Assert.AreEqual(0, EventBus.GetTotalBufferedEventCount(), "No events should be buffered");
            });
        }

        [Test]
        public void MarkAggregateEventTypeReady_EventsProcessedInOrder_ImmediateDispatcher()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var event1 = new RoutableTestEvent { AggregateId = aggregateId, Data = "First" };
            var event2 = new RoutableTestEvent { AggregateId = aggregateId, Data = "Second" };
            var event3 = new RoutableTestEvent { AggregateId = aggregateId, Data = "Third" };
            var processedOrder = new List<string>();

            EventBus.Send(event1);
            EventBus.Send(event2);
            EventBus.Send(event3);

            // Use ImmediateDispatcher to guarantee FIFO order processing
            EventBus.RegisterHandler<RoutableTestEvent>(evt => processedOrder.Add(evt.Data), aggregateId, new ImmediateDispatcher());

            // Act
            EventBus.AggregateReady(aggregateId);

            // Assert - Wait for async processing with ImmediateDispatcher
            Await.AtMost(TimeoutMs, () =>
            {
                Assert.AreEqual(3, processedOrder.Count, "Should have processed 3 events");
                Assert.AreEqual("First", processedOrder[0], "First event should be processed first");
                Assert.AreEqual("Second", processedOrder[1], "Second event should be processed second");
                Assert.AreEqual("Third", processedOrder[2], "Third event should be processed third");
            });
        }

        [UnityTest]
        public IEnumerator MarkAggregateEventTypeReady_EventsProcessedInOrder_UnityDispatcher()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var event1 = new RoutableTestEvent { AggregateId = aggregateId, Data = "First" };
            var event2 = new RoutableTestEvent { AggregateId = aggregateId, Data = "Second" };
            var event3 = new RoutableTestEvent { AggregateId = aggregateId, Data = "Third" };
            var processedOrder = new List<string>();

            EventBus.Send(event1);
            EventBus.Send(event2);
            EventBus.Send(event3);

            // Use UnityDispatcher - should maintain FIFO order while being asynchronous
            EventBus.RegisterHandler<RoutableTestEvent>(evt => processedOrder.Add(evt.Data), aggregateId, UnityDispatcher.Instance);

            // Act
            EventBus.AggregateReady(aggregateId);

            // Wait for Unity's Update() to process the queue
            yield return new WaitForSeconds(0.01f);

            // Assert - UnityDispatcher should process in FIFO order (async but ordered)
            Await.AtMost(TimeoutMs, () =>
            {
                Assert.AreEqual(3, processedOrder.Count, "Should have processed 3 events");
                Assert.AreEqual("First", processedOrder[0], "First event should be processed first");
                Assert.AreEqual("Second", processedOrder[1], "Second event should be processed second");
                Assert.AreEqual("Third", processedOrder[2], "Third event should be processed third");
            });
        }

        [Test]
        public void MarkAggregateEventTypeReady_EventsProcessedAsynchronously_AllEventsReceived()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var event1 = new RoutableTestEvent { AggregateId = aggregateId, Data = "Event 1" };
            var event2 = new RoutableTestEvent { AggregateId = aggregateId, Data = "Event 2" };
            var event3 = new RoutableTestEvent { AggregateId = aggregateId, Data = "Event 3" };
            var receivedEvents = new List<string>();

            EventBus.Send(event1);
            EventBus.Send(event2);
            EventBus.Send(event3);

            // Use ThreadPoolDispatcher to test async behavior
            EventBus.RegisterHandler<RoutableTestEvent>(evt => receivedEvents.Add(evt.Data), aggregateId, new ThreadPoolDispatcher());

            // Act
            EventBus.AggregateReady(aggregateId);

            // Assert - Wait for async processing (order may vary)
            Await.AtMost(2000, () =>
            {
                Assert.AreEqual(3, receivedEvents.Count, "Should have received all 3 events");
                Assert.Contains("Event 1", receivedEvents, "Should have received Event 1");
                Assert.Contains("Event 2", receivedEvents, "Should have received Event 2");
                Assert.Contains("Event 3", receivedEvents, "Should have received Event 3");
                Assert.AreEqual(0, EventBus.GetBufferedEventCount(aggregateId), "No events should be buffered");
            });
        }

        [Test]
        public void Send_HandlerRegisteredAfterBuffering_NewEventsAreProcessedAsynchronously()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var event1 = new RoutableTestEvent { AggregateId = aggregateId, Data = "Buffered Event" };
            var event2 = new RoutableTestEvent { AggregateId = aggregateId, Data = "New Event" };
            var handlerCalledCount = 0;

            // Send first event (should be buffered)
            EventBus.Send(event1);

            // Register handler
            EventBus.RegisterHandler<RoutableTestEvent>(_ => handlerCalledCount++, aggregateId, new ThreadPoolDispatcher());

            // Send second event (should not be buffered)
            EventBus.Send(event2);

            // Assert - Wait for async processing
            Await.AtMost(TimeoutMs, () =>
            {
                Assert.AreEqual(1, EventBus.GetBufferedEventCount(aggregateId), "Only first event should be buffered");
                Assert.AreEqual(1, handlerCalledCount, "Second event should have been processed");
            });
        }

        [Test]
        public void SendAndWait_RoutableEventWithHandler_ProcessesImmediately()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var testEvent = new RoutableTestEvent { AggregateId = aggregateId, Data = "Test Data" };
            var handlerCalled = false;

            EventBus.RegisterHandler<RoutableTestEvent>(_ => handlerCalled = true, aggregateId, new ImmediateDispatcher());

            // Act
            EventBus.SendAndWait(testEvent);

            // Assert - Should be processed immediately with ImmediateDispatcher
            Assert.IsTrue(handlerCalled, "Handler should have been called immediately");
            Assert.AreEqual(0, EventBus.GetBufferedEventCount(aggregateId), "No events should be buffered");
        }

        [Test]
        public void MarkAggregateEventTypeReady_EmptyGuid_LogsWarning()
        {
            // Arrange & Act
            EventBus.AggregateReady(Guid.Empty);

            // Assert
            Assert.AreEqual(0, EventBus.GetTotalBufferedEventCount());
        }

        [Test]
        public void GetBufferedEventCount_NonExistentAggregateId_ReturnsZero()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var count = EventBus.GetBufferedEventCount(nonExistentId);

            // Assert
            Assert.AreEqual(0, count);
        }

        [Test]
        public void GetBufferedAggregateIds_NoBufferedEvents_ReturnsEmptyCollection()
        {
            // Act
            var ids = EventBus.GetBufferedAggregateIds();

            // Assert
            Assert.IsEmpty(ids);
        }

        [Test]
        public void GetTotalBufferedEventCount_NoBufferedEvents_ReturnsZero()
        {
            // Act
            var count = EventBus.GetTotalBufferedEventCount();

            // Assert
            Assert.AreEqual(0, count);
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