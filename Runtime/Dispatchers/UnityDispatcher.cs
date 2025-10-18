#nullable enable
using System;
using System.Threading;
using UnityEngine;

namespace com.DvosTools.bus.Dispatchers
{
    public class UnityDispatcher : MonoBehaviour, IDispatcher
    {
        private static UnityDispatcher? _instance;
        private static readonly object Lock = new();
        private static SynchronizationContext? _mainThreadContext;

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

        public void Dispatch(Action? action, string? eventTypeName = null, Guid? aggregateId = null)
        {
            if (action != null)
            {
                try
                {
                    if (_mainThreadContext != null)
                    {
                        // Use SynchronizationContext to post to the main thread
                        _mainThreadContext.Post(_ => action.Invoke(), null);
                    }
                    else
                    {
                        _mainThreadContext = SynchronizationContext.Current;
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

    }
}