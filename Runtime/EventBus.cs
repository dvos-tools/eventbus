using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using com.DvosTools.bus.Runtime.Dispatchers;
using UnityEngine;

namespace com.DvosTools.bus.Runtime
{
    public class EventBus
    {
        private static EventBus _instance;
        public readonly Dictionary<Type, List<Subscription>> Handlers = new();
        public readonly Queue<QueuedEvent> EventQueue = new();
        public readonly object QueueLock = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly IDispatcher _mainThreadDispatcher = new UnityDispatcher();
        private readonly IDispatcher _backgroundDispatcher = new ThreadPoolDispatcher();

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
                    QueuedEvent eventToProcess = null;

                    lock (QueueLock)
                    {
                        if (EventQueue.Count > 0)
                        {
                            eventToProcess = EventQueue.Dequeue();
                        }
                    }

                    if (eventToProcess != null)
                    {
                        await ProcessEvent(eventToProcess);
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

        private async Task ProcessEvent(QueuedEvent queuedEvent)
        {
            Debug.Log($"[EventBus] Processing event: {queuedEvent.EventData} ({queuedEvent.EventType.Name}) queued at: {queuedEvent.QueuedAt}");

            var handlerInfos = Handlers[queuedEvent.EventType];

            // Separate handlers by thread requirement
            var backgroundHandlers = handlerInfos.Where(h => !h.RequiresMainThread).ToList();
            var mainThreadHandlers = handlerInfos.Where(h => h.RequiresMainThread).ToList();

            var tasks = new List<Task>();

            // Process background handlers concurrently
            if (backgroundHandlers.Any())
            {
                var backgroundTasks = backgroundHandlers.Select(async handlerInfo =>
                {
                    try
                    {
                        await Task.Run(() => handlerInfo.Handler(queuedEvent.EventData),
                            _cancellationTokenSource.Token);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(
                            $"[EventBus] Error in background handler for {queuedEvent.EventType.Name}: {ex.Message}");
                    }
                });
                tasks.AddRange(backgroundTasks);
            }

            // Process main thread handlers concurrently (dispatched to the main thread)
            if (mainThreadHandlers.Any())
            {
                var mainThreadTasks = mainThreadHandlers.Select(handlerInfo =>
                {
                    try
                    {
                        // Dispatch to Unity main thread
                        _mainThreadDispatcher.Dispatch(() => handlerInfo.Handler(queuedEvent.EventData));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(
                            $"[EventBus] Error in main thread handler for {queuedEvent.EventType.Name}: {ex.Message}");
                    }

                    return Task.CompletedTask;
                });
                tasks.AddRange(mainThreadTasks);
            }

            // Wait for all handlers to complete
            if (tasks.Any()) await Task.WhenAll(tasks);
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
                        if (handlerInfo.RequiresMainThread)
                            // Use dispatcher for main thread handlers
                            _mainThreadDispatcher.Dispatch(() => handlerInfo.Handler(eventData));
                        else
                            // Use background dispatcher for background handlers
                            _backgroundDispatcher.Dispatch(() => handlerInfo.Handler(eventData));
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
        public static void RegisterHandler<T>(Action<T> handler, bool requiresMainThread = true) where T : class
        {
            var eventType = typeof(T);
            var wrapper = new Action<object>(obj => handler((T)obj));
            var handlerInfo = new Subscription(wrapper, requiresMainThread);

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