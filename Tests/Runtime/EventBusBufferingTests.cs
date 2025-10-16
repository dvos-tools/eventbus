using System;
using System.Linq;
using System.Threading;
using com.DvosTools.bus.Dispatchers;
using NUnit.Framework;

namespace com.DvosTools.bus
{
    [TestFixture]
    public class EventBusBufferingTests
    {
        private const int TimeoutMs = 1000; // 1 second

        [SetUp]
        public void SetUp()
        {
            // Clear all handlers, buffered events, and queued events before each test
            EventBus.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            // Clear all handlers, buffered events, and queued events after each test
            EventBus.ClearAll();
        }

        [Test]
        public void Send_RoutableEventWithoutHandler_EventIsBuffered()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var testEvent = new RoutableTestEvent { AggregateId = aggregateId, Data = "Test Data" };

            // Act
            EventBus.Send(testEvent);

            // Assert
            Assert.AreEqual(1, EventBus.GetBufferedEventCount(aggregateId));
            Assert.AreEqual(1, EventBus.GetTotalBufferedEventCount());
            Assert.Contains(aggregateId, EventBus.GetBufferedAggregateIds().ToList());
        }

        [Test]
        public void Send_RoutableEventWithHandler_EventIsNotBuffered()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var testEvent = new RoutableTestEvent { AggregateId = aggregateId, Data = "Test Data" };
            var handlerCalled = false;

            EventBus.RegisterHandler<RoutableTestEvent>(evt => handlerCalled = true, aggregateId);

            // Act
            EventBus.Send(testEvent);

            // Assert
            Assert.AreEqual(0, EventBus.GetBufferedEventCount(aggregateId));
            Assert.AreEqual(0, EventBus.GetTotalBufferedEventCount());
            Assert.IsFalse(EventBus.GetBufferedAggregateIds().Contains(aggregateId));
        }

        [Test]
        public void Send_NonRoutableEvent_EventIsNotBuffered()
        {
            // Arrange
            var testEvent = new TestEvent { Message = "Test Message", Value = 42 };

            // Act
            EventBus.Send(testEvent);

            // Assert
            Assert.AreEqual(0, EventBus.GetTotalBufferedEventCount());
            Assert.IsEmpty(EventBus.GetBufferedAggregateIds());
        }

        [Test]
        public void Send_MultipleEventsForSameAggregateId_AllEventsAreBuffered()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var event1 = new RoutableTestEvent { AggregateId = aggregateId, Data = "Event 1" };
            var event2 = new RoutableTestEvent { AggregateId = aggregateId, Data = "Event 2" };
            var event3 = new RoutableTestEvent { AggregateId = aggregateId, Data = "Event 3" };

            // Act
            EventBus.Send(event1);
            EventBus.Send(event2);
            EventBus.Send(event3);

            // Assert
            Assert.AreEqual(3, EventBus.GetBufferedEventCount(aggregateId));
            Assert.AreEqual(3, EventBus.GetTotalBufferedEventCount());
        }

        [Test]
        public void Send_MultipleEventsForDifferentAggregateIds_EventsAreBufferedSeparately()
        {
            // Arrange
            var aggregateId1 = Guid.NewGuid();
            var aggregateId2 = Guid.NewGuid();
            var event1 = new RoutableTestEvent { AggregateId = aggregateId1, Data = "Event 1" };
            var event2 = new RoutableTestEvent { AggregateId = aggregateId2, Data = "Event 2" };
            var event3 = new RoutableTestEvent { AggregateId = aggregateId1, Data = "Event 3" };

            // Act
            EventBus.Send(event1);
            EventBus.Send(event2);
            EventBus.Send(event3);

            // Assert
            Assert.AreEqual(2, EventBus.GetBufferedEventCount(aggregateId1));
            Assert.AreEqual(1, EventBus.GetBufferedEventCount(aggregateId2));
            Assert.AreEqual(3, EventBus.GetTotalBufferedEventCount());
            
            var bufferedIds = EventBus.GetBufferedAggregateIds().ToList();
            Assert.Contains(aggregateId1, bufferedIds);
            Assert.Contains(aggregateId2, bufferedIds);
            Assert.AreEqual(2, bufferedIds.Count);
        }

        [Test]
        public void MarkAggregateEventTypeReady_WithBufferedEvents_EventsAreProcessed()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var event1 = new RoutableTestEvent { AggregateId = aggregateId, Data = "Event 1" };
            var event2 = new RoutableTestEvent { AggregateId = aggregateId, Data = "Event 2" };
            var handlerCalledCount = 0;
            var receivedData = new System.Collections.Generic.List<string>();

            EventBus.Send(event1);
            EventBus.Send(event2);

            EventBus.RegisterHandler<RoutableTestEvent>(evt => 
            {
                handlerCalledCount++;
                receivedData.Add(evt.Data);
            }, aggregateId, new ImmediateDispatcher());

            // Act
            EventBus.AggregateReady(aggregateId);

            // Assert - Wait for async processing
            Await.AtMost(TimeoutMs, () =>
            {
                Assert.AreEqual(2, handlerCalledCount);
                Assert.Contains("Event 1", receivedData);
                Assert.Contains("Event 2", receivedData);
                Assert.AreEqual(0, EventBus.GetBufferedEventCount(aggregateId));
                Assert.AreEqual(0, EventBus.GetTotalBufferedEventCount());
            });
        }

        [Test]
        public void MarkAggregateEventTypeReady_NoBufferedEvents_NoEventsProcessed()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var handlerCalled = false;

            EventBus.RegisterHandler<RoutableTestEvent>(evt => handlerCalled = true, aggregateId, new ImmediateDispatcher());

            // Act
            EventBus.AggregateReady(aggregateId);

            // Assert
            Assert.IsFalse(handlerCalled);
            Assert.AreEqual(0, EventBus.GetBufferedEventCount(aggregateId));
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
        public void MarkAggregateEventTypeReady_EventsProcessedInOrder()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var event1 = new RoutableTestEvent { AggregateId = aggregateId, Data = "First" };
            var event2 = new RoutableTestEvent { AggregateId = aggregateId, Data = "Second" };
            var event3 = new RoutableTestEvent { AggregateId = aggregateId, Data = "Third" };
            var processedOrder = new System.Collections.Generic.List<string>();

            EventBus.Send(event1);
            EventBus.Send(event2);
            EventBus.Send(event3);

            EventBus.RegisterHandler<RoutableTestEvent>(evt => processedOrder.Add(evt.Data), aggregateId, new ImmediateDispatcher());

            // Act
            EventBus.AggregateReady(aggregateId);

            // Assert - Wait for async processing
            Await.AtMost(TimeoutMs, () =>
            {
                Assert.AreEqual(3, processedOrder.Count);
                Assert.AreEqual("First", processedOrder[0]);
                Assert.AreEqual("Second", processedOrder[1]);
                Assert.AreEqual("Third", processedOrder[2]);
            });
        }

        [Test]
        public void SendAndWait_RoutableEventWithoutHandler_StillProcessesImmediately()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var testEvent = new RoutableTestEvent { AggregateId = aggregateId, Data = "Test Data" };

            // Act & Assert - Should not throw exception
            EventBus.SendAndWait(testEvent);

            // Assert - No buffering should occur with SendAndWait
            Assert.AreEqual(0, EventBus.GetBufferedEventCount(aggregateId));
            Assert.AreEqual(0, EventBus.GetTotalBufferedEventCount());
        }

        [Test]
        public void Send_MixedRoutableAndNonRoutableEvents_OnlyRoutableEventsAreBuffered()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var routableEvent = new RoutableTestEvent { AggregateId = aggregateId, Data = "Routable" };
            var nonRoutableEvent = new TestEvent { Message = "Non-Routable", Value = 42 };

            // Act
            EventBus.Send(routableEvent);
            EventBus.Send(nonRoutableEvent);

            // Assert
            Assert.AreEqual(1, EventBus.GetBufferedEventCount(aggregateId));
            Assert.AreEqual(1, EventBus.GetTotalBufferedEventCount());
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

        [Test]
        public void Send_HandlerRegisteredAfterBuffering_NewEventsAreNotBuffered()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var event1 = new RoutableTestEvent { AggregateId = aggregateId, Data = "Buffered Event" };
            var event2 = new RoutableTestEvent { AggregateId = aggregateId, Data = "New Event" };
            var handlerCalledCount = 0;

            // Send first event (should be buffered)
            EventBus.Send(event1);

            // Register handler
            EventBus.RegisterHandler<RoutableTestEvent>(evt => handlerCalledCount++, aggregateId, new ImmediateDispatcher());

            // Send second event (should not be buffered)
            EventBus.SendAndWait(event2);

            // Assert
            Assert.AreEqual(1, EventBus.GetBufferedEventCount(aggregateId)); // Only first event
            Assert.AreEqual(1, handlerCalledCount); // Second event was processed immediately
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