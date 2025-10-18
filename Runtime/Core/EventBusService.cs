#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using com.DvosTools.bus.Dispatchers;

namespace com.DvosTools.bus.Core
{
    /// <summary>
    /// Service layer for EventBus business logic.
    /// Handles complex operations, event processing, and business rules.
    /// </summary>
    internal class EventBusService
    {
        private readonly EventBusCore _core;
        
        public EventBusService(EventBusCore core)
        {
            _core = core ?? throw new ArgumentNullException(nameof(core));
        }

        /// <summary>
        /// Sends an event, handling routing and buffering logic.
        /// </summary>
        public void Send<T>(T eventData) where T : class
        {
            var queuedEvent = new QueuedEvent(eventData, typeof(T), DateTime.UtcNow);
            var routedEvent = RoutedQueuedEvent.FromQueuedEvent(queuedEvent);
            
            // Handle non-routed events (no aggregate ID)
            if (routedEvent.AggregateId == Guid.Empty)
            {
                QueueEvent(queuedEvent, typeof(T).Name);
                return;
            }
            
            // Handle routed events - check if handler exists
            if (HasHandlerForAggregate(typeof(T), routedEvent.AggregateId))
            {
                QueueEvent(queuedEvent, typeof(T).Name);
                return;
            }
            
            // No handler found - buffer the event
            BufferEvent(queuedEvent, routedEvent.AggregateId, typeof(T).Name);
        }

        /// <summary>
        /// Sends an event and waits for all handlers to complete.
        /// </summary>
        public void SendAndWait<T>(T eventData) where T : class
        {
            var eventType = typeof(T);
            var routedEvent = RoutedQueuedEvent.FromQueuedEvent(new QueuedEvent(eventData, eventType));

            lock (_core.BufferedEventsLock)
            {
                if (_core.Handlers.TryGetValue(eventType, out var handlerInfos))
                {
                    foreach (var handlerInfo in handlerInfos)
                        ProcessHandlerAndWait(handlerInfo, eventData, eventType.Name, routedEvent.AggregateId);
                }
                else 
                    EventBusLogger.LogWarning($"No handlers for {eventType.Name}");
            }
        }

        /// <summary>
        /// Processes buffered events when an aggregate becomes ready.
        /// </summary>
        public void AggregateReady(Guid aggregateId)
        {
            if (aggregateId == Guid.Empty)
            {
                EventBusLogger.LogWarning("Cannot mark empty aggregate ID as ready");
                return;
            }

            var eventsToProcess = ExtractBufferedEvents(aggregateId);
            if (eventsToProcess.Count == 0)
            {
                EventBusLogger.Log($"No buffered events found for aggregate ID {aggregateId}");
                return;
            }

            ProcessBufferedEvents(eventsToProcess, aggregateId);
        }

        /// <summary>
        /// Processes a single event by finding and executing appropriate handlers.
        /// </summary>
        public void ProcessEvent(QueuedEvent queuedEvent)
        {
            var routedEvent = RoutedQueuedEvent.FromQueuedEvent(queuedEvent);

            lock (_core.BufferedEventsLock)
            {
                if (_core.Handlers.TryGetValue(queuedEvent.EventType, out var handlerInfos))
                {
                    foreach (var handlerInfo in handlerInfos)
                        ProcessHandler(handlerInfo, queuedEvent.EventData, queuedEvent.EventType.Name, routedEvent.AggregateId);
                }
                else 
                    EventBusLogger.LogWarning($"No handlers for {queuedEvent.EventType.Name}");
            }
        }

        /// <summary>
        /// Registers a handler for a specific event type.
        /// </summary>
        public static void RegisterHandler<T>(Action<T> handler, Guid aggregateId = default, IDispatcher? dispatcher = null) where T : class
        {
            var core = EventBusCore.Instance;
            var eventType = typeof(T);
            var wrapper = new Action<object>(obj => handler((T)obj));

            var customDispatcher = dispatcher ?? new ThreadPoolDispatcher();
            var subscription = new Subscription(wrapper, customDispatcher, aggregateId);

            if (!core.Handlers.ContainsKey(eventType))
                core.Handlers[eventType] = new List<Subscription>();

            core.Handlers[eventType].Add(subscription);

            EventBusLogger.Log(aggregateId != Guid.Empty
                ? $"Registered routed handler for {eventType.Name} (ID: {aggregateId})"
                : $"Registered handler for {eventType.Name}");
        }
        

        private bool HasHandlerForAggregate(Type eventType, Guid aggregateId)
        {
            lock (_core.BufferedEventsLock)
            {
                if (!_core.Handlers.TryGetValue(eventType, out var handlerInfos))
                    return false;

                return handlerInfos.Any(handler => handler.AggregateId == aggregateId);
            }
        }

        private void QueueEvent(QueuedEvent queuedEvent, string eventTypeName)
        {
            lock (_core.QueueLock)
                _core.EventQueue.Enqueue(queuedEvent);
            
            EventBusLogger.Log($"Queued {eventTypeName}");
        }

        private void BufferEvent(QueuedEvent queuedEvent, Guid aggregateId, string eventTypeName)
        {
            lock (_core.BufferedEventsLock)
            {
                if (!_core.BufferedEvents.ContainsKey(aggregateId))
                    _core.BufferedEvents[aggregateId] = new Queue<QueuedEvent>();
                
                _core.BufferedEvents[aggregateId].Enqueue(queuedEvent);
            }
            
            EventBusLogger.Log($"Buffered {eventTypeName} for aggregate ID {aggregateId} (no handler available)");
        }

        private void ProcessHandler(Subscription subscription, object eventData, string eventTypeName, Guid eventAggregateId)
        {
            // Check if this handler has an aggregate ID requirement
            if (subscription.AggregateId != Guid.Empty)
            {
                // only process if aggregate IDs match
                if (eventAggregateId != Guid.Empty && subscription.AggregateId == eventAggregateId)
                    subscription.Dispatcher.Dispatch(() => subscription.Handler(eventData), eventTypeName, eventAggregateId);
                
                return;
            }

            // This is a regular handler - process all events
            subscription.Dispatcher.Dispatch(() => subscription.Handler(eventData), eventTypeName);
        }

        private void ProcessHandlerAndWait(Subscription subscription, object eventData, string eventTypeName, Guid eventAggregateId)
        {
            // Check if this handler has an aggregate ID requirement
            if (subscription.AggregateId != Guid.Empty)
            {
                // only process if aggregate IDs match
                if (eventAggregateId != Guid.Empty && subscription.AggregateId == eventAggregateId)
                    subscription.Dispatcher.DispatchAndWait(() => subscription.Handler(eventData), eventTypeName, eventAggregateId);
                
                return;
            }

            // This is a regular handler - process all events
            subscription.Dispatcher.DispatchAndWait(() => subscription.Handler(eventData), eventTypeName);
        }


        private List<QueuedEvent> ExtractBufferedEvents(Guid aggregateId)
        {
            lock (_core.BufferedEventsLock)
            {
                if (!_core.BufferedEvents.TryGetValue(aggregateId, out var bufferedQueue))
                    return new List<QueuedEvent>();

                var eventsToProcess = new List<QueuedEvent>();
                while (bufferedQueue.Count > 0)
                {
                    eventsToProcess.Add(bufferedQueue.Dequeue());
                }
                
                _core.BufferedEvents.Remove(aggregateId);
                return eventsToProcess;
            }
        }

        private void ProcessBufferedEvents(List<QueuedEvent> eventsToProcess, Guid aggregateId)
        {
            // Queue the events for processing by the background queue processor
            // This ensures they go through the same processing flow as normal events
            lock (_core.QueueLock)
            {
                foreach (var eventToProcess in eventsToProcess)
                {
                    _core.EventQueue.Enqueue(eventToProcess);
                }
            }
            
            EventBusLogger.Log($"Queued {eventsToProcess.Count} buffered events for processing (aggregate ID: {aggregateId})");
        }
        
    }
}