#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using com.DvosTools.bus.Dispatchers;

namespace com.DvosTools.bus.Core
{
    public class EventBusCore
    {
        private static EventBusCore? _instance;
        public readonly Dictionary<Type, List<Subscription>> Handlers = new();
        public readonly Queue<QueuedEvent> EventQueue = new();
        public readonly Dictionary<Guid, Queue<QueuedEvent>> BufferedEvents = new();
        public readonly object QueueLock = new();
        private readonly object _bufferedEventsLock = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        
        // Maximum number of events to process in one batch before yielding
        private const int MaxBatchSize = 100;
        

        public static EventBusCore Instance => _instance ??= new EventBusCore();

        private EventBusCore()
        {
            _ = Task.Run(ProcessEventQueueAsync, _cancellationTokenSource.Token); // Start the background queue processor
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

        private bool HasHandlerForAggregate(Type eventType, Guid aggregateId)
        {
            if (!Handlers.TryGetValue(eventType, out var handlerInfos))
                return false;
                
            return handlerInfos.Any(handler => handler.AggregateId == aggregateId);
        }

        private void QueueEvent(QueuedEvent queuedEvent, string eventTypeName)
        {
            lock (QueueLock)
                EventQueue.Enqueue(queuedEvent);
            
            EventBusLogger.Log($"Queued {eventTypeName}");
        }

        private void BufferEvent(QueuedEvent queuedEvent, Guid aggregateId, string eventTypeName)
        {
            lock (_bufferedEventsLock)
            {
                if (!BufferedEvents.ContainsKey(aggregateId))
                    BufferedEvents[aggregateId] = new Queue<QueuedEvent>();
                
                BufferedEvents[aggregateId].Enqueue(queuedEvent);
            }
            
            EventBusLogger.Log($"Buffered {eventTypeName} for aggregate ID {aggregateId} (no handler available)");
        }


        private async Task ProcessEventQueueAsync()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var eventsToProcess = new List<QueuedEvent>();
                    
                    // Collect up to MaxBatchSize events
                    lock (QueueLock)
                    {
                        var count = Math.Min(EventQueue.Count, MaxBatchSize);
                        for (int i = 0; i < count; i++)
                            if (EventQueue.Count > 0)
                                eventsToProcess.Add(EventQueue.Dequeue());
                    }

                    if (eventsToProcess.Count > 0)
                    {
                        foreach (var eventToProcess in eventsToProcess)
                            ProcessEvent(eventToProcess);
                    }
                    else
                    {
                        await Task.Yield();
                    }
                }
                catch (OperationCanceledException)
                {
                    break; // Expected when cancellation is requested
                }
                catch (Exception ex)
                {
                    EventBusLogger.LogError($"Queue processor error: {ex.Message}");
                }
            }
        }

        private void ProcessEvent(QueuedEvent queuedEvent)
        {
            var routedEvent = RoutedQueuedEvent.FromQueuedEvent(queuedEvent);
            
            if (Handlers.TryGetValue(queuedEvent.EventType, out var handlerInfos))
            {
                foreach (var handlerInfo in handlerInfos)
                    ProcessHandler(handlerInfo, queuedEvent.EventData, queuedEvent.EventType.Name, routedEvent.AggregateId);
            }
            else 
                EventBusLogger.LogWarning($"No handlers for {queuedEvent.EventType.Name}");
        }

        private void ProcessHandler(Subscription subscription, object eventData, string eventTypeName, Guid eventAggregateId)
        {
            // Check if this handler has an aggregate ID requirement
            if (subscription.AggregateId != Guid.Empty)
            {
                // only process if aggregate IDs match
                if (eventAggregateId != Guid.Empty && subscription.AggregateId == eventAggregateId)
                    ExecuteRoutedHandler(subscription, eventData, eventTypeName, eventAggregateId);
                
                return;
            }

            // This is a regular handler - process all events
            ExecuteHandler(subscription, eventData, eventTypeName);
        }

        private void ExecuteHandler(Subscription subscription, object eventData, string eventTypeName)
        {
            try
            {
                subscription.Dispatcher.Dispatch(() => subscription.Handler(eventData));
            }
            catch (Exception ex)
            {
                EventBusLogger.LogError($"Handler error for {eventTypeName}: {ex.Message}");
            }
        }

        private void ExecuteRoutedHandler(Subscription subscription, object eventData, string eventTypeName, Guid aggregateId)
        {
            try
            {
                subscription.Dispatcher.Dispatch(() => subscription.Handler(eventData));
            }
            catch (Exception ex)
            {
                EventBusLogger.LogError($"Routed handler error for {eventTypeName} (ID: {aggregateId}): {ex.Message}");
            }
        }

        private void ExecuteHandlerAndWait(Subscription subscription, object eventData, string eventTypeName)
        {
            try
            {
                subscription.Dispatcher.DispatchAndWait(() => subscription.Handler(eventData));
            }
            catch (Exception ex)
            {
                EventBusLogger.LogError($"Handler error for {eventTypeName}: {ex.Message}");
            }
        }

        private void ExecuteRoutedHandlerAndWait(Subscription subscription, object eventData, string eventTypeName, Guid aggregateId)
        {
            try
            {
                subscription.Dispatcher.DispatchAndWait(() => subscription.Handler(eventData));
            }
            catch (Exception ex)
            {
                EventBusLogger.LogError($"Routed handler error for {eventTypeName} (ID: {aggregateId}): {ex.Message}");
            }
        }

        public void SendAndWait<T>(T eventData) where T : class
        {
            var eventType = typeof(T);
            var routedEvent = RoutedQueuedEvent.FromQueuedEvent(new QueuedEvent(eventData, eventType));

            if (Handlers.TryGetValue(eventType, out var handlerInfos))
            {
                foreach (var handlerInfo in handlerInfos)
                    ProcessHandlerAndWait(handlerInfo, eventData, eventType.Name, routedEvent.AggregateId);
            }
            else 
                EventBusLogger.LogWarning($"No handlers for {eventType.Name}");
        }

        private void ProcessHandlerAndWait(Subscription subscription, object eventData, string eventTypeName, Guid eventAggregateId)
        {
            // Check if this handler has an aggregate ID requirement
            if (subscription.AggregateId != Guid.Empty)
            {
                // only process if aggregate IDs match
                if (eventAggregateId != Guid.Empty && subscription.AggregateId == eventAggregateId)
                    ExecuteRoutedHandlerAndWait(subscription, eventData, eventTypeName, eventAggregateId);
                
                return;
            }

            // This is a regular handler - process all events
            ExecuteHandlerAndWait(subscription, eventData, eventTypeName);
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

            ProcessBufferedEvents(eventsToProcess, aggregateId);
        }

        private List<QueuedEvent> ExtractBufferedEvents(Guid aggregateId)
        {
            lock (_bufferedEventsLock)
            {
                if (!BufferedEvents.TryGetValue(aggregateId, out var bufferedQueue))
                    return new List<QueuedEvent>();

                var eventsToProcess = new List<QueuedEvent>();
                while (bufferedQueue.Count > 0)
                {
                    eventsToProcess.Add(bufferedQueue.Dequeue());
                }
                
                BufferedEvents.Remove(aggregateId);
                return eventsToProcess;
            }
        }

        private void ProcessBufferedEvents(List<QueuedEvent> eventsToProcess, Guid aggregateId)
        {
            // Queue the events for processing by the background queue processor
            // This ensures they go through the same processing flow as normal events
            lock (QueueLock)
            {
                foreach (var eventToProcess in eventsToProcess)
                {
                    EventQueue.Enqueue(eventToProcess);
                }
            }
            
            EventBusLogger.Log($"Queued {eventsToProcess.Count} buffered events for processing (aggregate ID: {aggregateId})");
        }


        public int GetBufferedEventCount(Guid aggregateId)
        {
            lock (_bufferedEventsLock)
            {
                return BufferedEvents.TryGetValue(aggregateId, out var bufferedQueue) ? bufferedQueue.Count : 0;
            }
        }

        public IEnumerable<Guid> GetBufferedAggregateIds()
        {
            lock (_bufferedEventsLock)
            {
                return BufferedEvents.Keys.ToList();
            }
        }

        public int GetTotalBufferedEventCount()
        {
            lock (_bufferedEventsLock)
            {
                return BufferedEvents.Values.Sum(queue => queue.Count);
            }
        }

        public static void RegisterHandler<T>(Action<T> handler, Guid aggregateId = default, IDispatcher? dispatcher = null) where T : class
        {
            var eventType = typeof(T);
            var wrapper = new Action<object>(obj => handler((T)obj));

            var customDispatcher = dispatcher ?? new ThreadPoolDispatcher();
            var subscription = new Subscription(wrapper, customDispatcher, aggregateId);

            if (!Instance.Handlers.ContainsKey(eventType))
                Instance.Handlers[eventType] = new List<Subscription>();

            Instance.Handlers[eventType].Add(subscription);

            EventBusLogger.Log(aggregateId != Guid.Empty
                ? $"Registered routed handler for {eventType.Name} (ID: {aggregateId})"
                : $"Registered handler for {eventType.Name}");
        }

        [Obsolete("Use RegisterHandler instead. This method is provided for backwards compatibility.")]
        public static void RegisterStaticHandler<T>(Action<T> handler, IDispatcher? dispatcher = null) where T : class
        {
            RegisterHandler(handler, Guid.Empty, dispatcher);
        }

        public void Shutdown()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

    }
}