#nullable enable
using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace com.DvosTools.bus.Dispatchers
{
    public class UnityDispatcher : MonoBehaviour, IDispatcher
    {
        private static UnityDispatcher? _instance;
        private static readonly object Lock = new();
        private static SynchronizationContext? _mainThreadContext;
        private static readonly ConcurrentQueue<Action> ActionQueue = new();

        public static UnityDispatcher? Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Lock)
                    {
                        if (_instance == null)
                        {
                            var go = new GameObject(nameof(UnityDispatcher));
                            _instance = go.AddComponent<UnityDispatcher>();
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            // Capture the main thread context
            _mainThreadContext = SynchronizationContext.Current;
        }

        private void Update()
        {
            // Process queued actions sequentially on the main thread
            ProcessActionQueue();
        }

        private static void ProcessActionQueue()
        {
            var processedCount = 0;
            // Process all queued actions in order
            while (ActionQueue.TryDequeue(out var action))
            {
                try
                {
                    action?.Invoke();
                    processedCount++;
                }
                catch (Exception ex)
                {
                    EventBusLogger.LogError($"Error executing action on main thread: {ex.Message}");
                }
            }
            
            if (processedCount > 0)
            {
                EventBusLogger.Log($"UnityDispatcher processed {processedCount} actions");
            }
        }


        public void Dispatch(Action? action, string? eventTypeName = null, Guid? aggregateId = null)
        {
            if (action != null)
            {
                try
                {
                    EventBusLogger.Log($"UnityDispatcher.Dispatch called for {eventTypeName}");
                    // Always queue the action for sequential processing
                    ActionQueue.Enqueue(() =>
                    {
                        try
                        {
                            action.Invoke();
                        }
                        catch (Exception ex)
                        {
                            var errorMessage = aggregateId.HasValue 
                                ? $"Routed handler error for {eventTypeName} (ID: {aggregateId}): {ex.Message}"
                                : $"Handler error for {eventTypeName}: {ex.Message}";
                            EventBusLogger.LogError(errorMessage);
                        }
                    });
                    
                }
                catch (Exception ex)
                {
                    var errorMessage = aggregateId.HasValue 
                        ? $"Routed handler error for {eventTypeName} (ID: {aggregateId}): {ex.Message}"
                        : $"Handler error for {eventTypeName}: {ex.Message}";
                    EventBusLogger.LogError(errorMessage);
                }
            }
        }

        public void DispatchAndWait(Action? action, string? eventTypeName = null, Guid? aggregateId = null)
        {
            if (action != null)
            {
                try
                {
                    if (_mainThreadContext != null)
                    {
                        // Use SynchronizationContext to send (synchronous) to the main thread
                        _mainThreadContext.Send(_ => action.Invoke(), null);
                    }
                    else
                    {
                        _mainThreadContext = SynchronizationContext.Current;
                        // Fallback to immediate execution if no context
                        action.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    var errorMessage = aggregateId.HasValue 
                        ? $"Routed handler error for {eventTypeName} (ID: {aggregateId}): {ex.Message}"
                        : $"Handler error for {eventTypeName}: {ex.Message}";
                    EventBusLogger.LogError(errorMessage);
                }
            }
        }

        private void OnDestroy()
        {
            // Clear any remaining actions when the dispatcher is destroyed
            while (ActionQueue.TryDequeue(out _)) { }
        }

    }
}