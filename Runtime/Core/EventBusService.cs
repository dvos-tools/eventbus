#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using com.DvosTools.bus.Dispatchers;

namespace com.DvosTools.bus.Core
{
    internal class EventBusService
    {
        private readonly EventBusCore _core;
        
        public EventBusService(EventBusCore core)
        {
            _core = core ?? throw new ArgumentNullException(nameof(core));
        }

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

        public void SendAndWait<T>(T eventData) where T : class
        {
            var eventType = typeof(T);
            var routedEvent = RoutedQueuedEvent.FromQueuedEvent(new QueuedEvent(eventData, eventType));

            List<Subscription> handlerInfos;
            lock (_core.HandlersLock)
            {
                if (_core.Handlers.TryGetValue(eventType, out handlerInfos))
                {
                    handlerInfos = new List<Subscription>(handlerInfos); // Copy to avoid holding lock during async operations
                }
                else 
                {
                    EventBusLogger.LogWarning($"No handlers for {eventType.Name}");
                    return;
                }
            }

            // Process handlers sequentially to maintain FIFO order
            foreach (var handlerInfo in handlerInfos)
            {
                ProcessHandlerAndWaitAsync(handlerInfo, eventData, eventType.Name, routedEvent.AggregateId).Wait();
            }
        }

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

            // Process buffered events immediately, and in order
            // This ensures they are processed as a batch in the exact order they were buffered
            foreach (var eventToProcess in eventsToProcess)
            {
                ProcessEventImmediately(eventToProcess);
            }
            
            EventBusLogger.Log($"Processed {eventsToProcess.Count} buffered events immediately (aggregate ID: {aggregateId})");
        }

        public async Task ProcessEventAsync(QueuedEvent queuedEvent)
        {
            var routedEvent = RoutedQueuedEvent.FromQueuedEvent(queuedEvent);

            List<Subscription> handlerInfos;
            lock (_core.HandlersLock)
            {
                if (_core.Handlers.TryGetValue(queuedEvent.EventType, out handlerInfos))
                {
                    handlerInfos = new List<Subscription>(handlerInfos); // Copy to avoid holding lock during async operations
                }
                else 
                {
                    EventBusLogger.LogWarning($"No handlers for {queuedEvent.EventType.Name}");
                    return;
                }
            }

            // Process handlers sequentially to maintain FIFO order
            foreach (var handlerInfo in handlerInfos)
            {
                await ProcessHandlerAndWaitAsync(handlerInfo, queuedEvent.EventData, queuedEvent.EventType.Name, routedEvent.AggregateId);
            }
        }


        public static void RegisterHandler<T>(Action<T> handler, Guid aggregateId = default, IDispatcher? dispatcher = null) where T : class
        {
            var core = EventBusCore.Instance;
            var eventType = typeof(T);
            var wrapper = new Action<object>(obj => handler((T)obj));

            var customDispatcher = dispatcher ?? new ThreadPoolDispatcher();
            var subscription = new Subscription(wrapper, customDispatcher, aggregateId, handler);
            EventBusLogger.Log($"Registered handler: {handler?.GetHashCode()} for aggregate {aggregateId}");

            lock (core.HandlersLock)
            {
                if (!core.Handlers.ContainsKey(eventType))
                    core.Handlers[eventType] = new List<Subscription>();

                core.Handlers[eventType].Add(subscription);
            }

            EventBusLogger.Log(aggregateId != Guid.Empty
                ? $"Registered routed handler for {eventType.Name} (ID: {aggregateId})"
                : $"Registered handler for {eventType.Name}");
        }
        

        private bool HasHandlerForAggregate(Type eventType, Guid aggregateId)
        {
            lock (_core.HandlersLock)
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

        private async Task ProcessHandlerAndWaitAsync(Subscription subscription, object eventData, string eventTypeName, Guid eventAggregateId)
        {
            // Check if this handler has an aggregate ID requirement
            if (subscription.AggregateId != Guid.Empty)
            {
                // only process if aggregate IDs match
                if (eventAggregateId != Guid.Empty && subscription.AggregateId == eventAggregateId)
                {
                    await subscription.Dispatcher.DispatchAndWaitAsync(() => subscription.Handler(eventData), eventTypeName, eventAggregateId);
                }
                
                return;
            }

            await subscription.Dispatcher.DispatchAndWaitAsync(() => subscription.Handler(eventData), eventTypeName);
        }

        public void ProcessEventImmediately(QueuedEvent queuedEvent)
        {
            var routedEvent = RoutedQueuedEvent.FromQueuedEvent(queuedEvent);

            List<Subscription> handlerInfos;
            lock (_core.HandlersLock)
            {
                if (_core.Handlers.TryGetValue(queuedEvent.EventType, out handlerInfos))
                {
                    handlerInfos = new List<Subscription>(handlerInfos); // Copy to avoid holding lock during processing
                }
                else 
                {
                    EventBusLogger.LogWarning($"No handlers for {queuedEvent.EventType.Name}");
                    return;
                }
            }

            // Process handlers immediately and sequentially to maintain FIFO order
            foreach (var handlerInfo in handlerInfos)
            {
                ProcessHandlerImmediately(handlerInfo, queuedEvent.EventData, queuedEvent.EventType.Name, routedEvent.AggregateId);
            }
        }

        private void ProcessHandlerImmediately(Subscription subscription, object eventData, string eventTypeName, Guid eventAggregateId)
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

        public void DisposeHandlerFromAggregate<T>(Action<T> handler, Guid aggregateId) where T : class
        {
            lock (_core.QueueLock)
            {
                _core.DisposeHandlerFromAggregate(handler, aggregateId);
            }
        }
        
    }
}