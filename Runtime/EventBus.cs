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
        private const int MaxBatchSize = 10;

        public static EventBus Instance => _instance ??= new EventBus();

        private EventBus()
        {
            // Start the background queue processor
            _ = Task.Run(ProcessEventQueueAsync, _cancellationTokenSource.Token);
        }

        public void Send<T>(T eventData) where T : class
        {
            var queuedEvent = new QueuedEvent(
                eventData,
                typeof(T),
                DateTime.UtcNow
            );

            lock (QueueLock)
            {
                EventQueue.Enqueue(queuedEvent);
            }

            Debug.Log($"[EventBus] Queued event: {eventData} ({typeof(T).Name}) at {queuedEvent.QueuedAt}");
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
                        {
                            if (EventQueue.Count > 0)
                            {
                                eventsToProcess.Add(EventQueue.Dequeue());
                            }
                        }
                    }

                    if (eventsToProcess.Count > 0)
                    {
                        foreach (var eventToProcess in eventsToProcess)
                        {
                            ProcessEvent(eventToProcess);
                        }
                    }
                    else
                    {
                        // No events available, yield control briefly then continue
                        await Task.Yield();
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventBus] Error in queue processor: {ex.Message}");
                }
            }
        }

        private void ProcessEvent(QueuedEvent queuedEvent)
        {
            Debug.Log($"[EventBus] Processing event: {queuedEvent.EventData} ({queuedEvent.EventType.Name}) queued at: {queuedEvent.QueuedAt}");

            if (!Handlers.TryGetValue(queuedEvent.EventType, out var handlerInfos))
            {
                Debug.LogWarning($"[EventBus] No handlers found for event type: {queuedEvent.EventType.Name}");
                return;
            }

            // Process all handlers using their assigned dispatchers
            foreach (var handlerInfo in handlerInfos)
            {
                try
                {
                    // Use the dispatcher assigned to this handler
                    handlerInfo.Dispatcher.Dispatch(() => handlerInfo.Handler(queuedEvent.EventData));
                }
                catch (Exception ex)
                {
                    Debug.LogError(
                        $"[EventBus] Error in handler for {queuedEvent.EventType.Name}: {ex.Message}");
                }
            }
        }


        public void SendAndWait<T>(T eventData) where T : class
        {
            var eventType = typeof(T);
            Debug.Log($"[EventBus] Publishing event: {eventData} ({eventType.Name})");

            if (Handlers.TryGetValue(eventType, out var handlerInfos))
            {
                foreach (var handlerInfo in handlerInfos)
                {
                    try
                    {
                        // Use the dispatcher assigned to this handler
                        handlerInfo.Dispatcher.Dispatch(() => handlerInfo.Handler(eventData));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[EventBus] Error in handler for {eventType.Name}: {ex.Message}");
                    }
                }

                return;
            }

            Debug.LogWarning($"[EventBus] No handlers found for event type: {eventType.Name}");
        }

        // Static method for handlers to register themselves
        public static void RegisterStaticHandler<T>(Action<T> handler, IDispatcher? dispatcher = null) where T : class
        {
            var eventType = typeof(T);
            var wrapper = new Action<object>(obj => handler((T)obj));
            
            var customDispatcher = dispatcher ?? new ThreadPoolDispatcher();
            var handlerInfo = new Subscription(wrapper, customDispatcher);

            if (!Instance.Handlers.ContainsKey(eventType))
                Instance.Handlers[eventType] = new List<Subscription>();

            Instance.Handlers[eventType].Add(handlerInfo);
        }

        public void Shutdown()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

    }
}