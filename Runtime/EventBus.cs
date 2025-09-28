using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using com.DvosTools.bus.Runtime.Dispatchers;
using UnityEngine;

namespace com.DvosTools.bus.Runtime
{
    public class EventBus
    {
        private static EventBus? _instance;
        public readonly Dictionary<Type, List<Subscription>> Handlers = new();
        public readonly Queue<QueuedEvent> EventQueue = new();
        public readonly object QueueLock = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        
        // Maximum number of events to process in one batch before yielding
        private const int MaxBatchSize = 100;
        
        // Logging configuration
        public static bool EnableLogging { get; set; } = false;

        public static EventBus Instance => _instance ??= new EventBus();

        private EventBus()
        {
            _ = Task.Run(ProcessEventQueueAsync, _cancellationTokenSource.Token); // Start the background queue processor
        }

        private static void Log(string message)
        {
            if (EnableLogging)
            {
                Debug.Log($"[EventBus] {message}");
            }
        }

        private static void LogWarning(string message)
        {
            if (EnableLogging)
            {
                Debug.LogWarning($"[EventBus] {message}");
            }
        }

        private static void LogError(string message)
        {
            Debug.LogError($"[EventBus] {message}");
        }

        public void Send<T>(T eventData) where T : class
        {
            var queuedEvent = new QueuedEvent(
                eventData,
                typeof(T),
                DateTime.UtcNow
            );

            lock (QueueLock)
                EventQueue.Enqueue(queuedEvent);

            Log($"Queued {typeof(T).Name}");
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
                    else await Task.Yield();
                }
                catch (OperationCanceledException)
                {
                    break; // Expected when cancellation is requested
                }
                catch (Exception ex)
                {
                    LogError($"Queue processor error: {ex.Message}");
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
                LogWarning($"No handlers for {queuedEvent.EventType.Name}");
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
                LogError($"Handler error for {eventTypeName}: {ex.Message}");
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
                LogError($"Routed handler error for {eventTypeName} (ID: {aggregateId}): {ex.Message}");
            }
        }


        public void SendAndWait<T>(T eventData) where T : class
        {
            var eventType = typeof(T);
            var routedEvent = RoutedQueuedEvent.FromQueuedEvent(new QueuedEvent(eventData, eventType));

            if (Handlers.TryGetValue(eventType, out var handlerInfos))
            {
                foreach (var handlerInfo in handlerInfos)
                    ProcessHandler(handlerInfo, eventData, eventType.Name, routedEvent.AggregateId);
            }
            else 
                LogWarning($"No handlers for {eventType.Name}");
        }

        /// <summary>
        /// Registers a handler for the specified event type.
        /// If aggregateId is provided (not Guid.Empty), the handler will only receive events with matching aggregate IDs.
        /// If aggregateId is Guid.Empty, the handler will receive all events of the specified type.
        /// </summary>
        /// <typeparam name="T">The event type to handle</typeparam>
        /// <param name="handler">The event handler</param>
        /// <param name="aggregateId">Optional aggregate ID for routing. Use Guid.Empty for regular handlers.</param>
        /// <param name="dispatcher">Optional custom dispatcher for executing the handler</param>
        public static void RegisterHandler<T>(Action<T> handler, Guid aggregateId = default, IDispatcher? dispatcher = null) where T : class
        {
            var eventType = typeof(T);
            var wrapper = new Action<object>(obj => handler((T)obj));
            
            var customDispatcher = dispatcher ?? new ThreadPoolDispatcher();
            var subscription = new Subscription(wrapper, customDispatcher, aggregateId);

            if (!Instance.Handlers.ContainsKey(eventType))
                Instance.Handlers[eventType] = new List<Subscription>();

            Instance.Handlers[eventType].Add(subscription);
            
            if (aggregateId != Guid.Empty)
            {
                Log($"Registered routed handler for {eventType.Name} (ID: {aggregateId})");
            }
            else
            {
                Log($"Registered handler for {eventType.Name}");
            }
        }

        public void Shutdown()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

    }
}