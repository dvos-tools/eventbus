#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace com.DvosTools.bus.Dispatchers
{
    internal class QueuedAction
    {
        public Action Action { get; }
        public TaskCompletionSource<bool> CompletionSource { get; }
        public string? EventTypeName { get; }
        public Guid? AggregateId { get; }

        public QueuedAction(Action action, string? eventTypeName = null, Guid? aggregateId = null)
        {
            Action = action ?? throw new ArgumentNullException(nameof(action));
            CompletionSource = new TaskCompletionSource<bool>();
            EventTypeName = eventTypeName;
            AggregateId = aggregateId;
        }
    }

    public class UnityDispatcher : MonoBehaviour, IDispatcher
    {
        private static UnityDispatcher? _instance;
        private static readonly object Lock = new();
        private static SynchronizationContext? _mainThreadContext;
        private static readonly Queue<QueuedAction> _actionQueue = new();
        private static readonly object _queueLock = new();

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

        public bool IsQueueEmpty()
        {
            lock (_queueLock)
            {
                return _actionQueue.Count == 0;
            }
        }

        public async Task DispatchAndWaitAsync(Action? action, string? eventTypeName = null, Guid? aggregateId = null)
        {
            if (action == null) return;
            try
            {
                if (SynchronizationContext.Current == _mainThreadContext)
                {
                    // Already on the main thread, execute immediately
                    action.Invoke();
                }
                else
                {
                    // On background thread, queue the action and wait for completion
                    // This maintains FIFO order by ensuring each action completes before the next
                    var queuedAction = new QueuedAction(action, eventTypeName, aggregateId);
                    
                    lock (_queueLock)
                    {
                        _actionQueue.Enqueue(queuedAction);
                    }
                    
                    // Wait for the action to complete on the main thread
                    // This ensures FIFO order - each event waits for the previous one to complete
                    await queuedAction.CompletionSource.Task;
                }
            }
            catch (Exception ex)
            {
                var errorMessage = aggregateId.HasValue 
                    ? $"DispatchAndWaitAsync error for {eventTypeName} (ID: {aggregateId}): {ex.Message}"
                    : $"DispatchAndWaitAsync error for {eventTypeName}: {ex.Message}";
                EventBusLogger.LogError(errorMessage);
            }
        }


        private void Update()
        {
            // Process one action per frame to maintain FIFO order and prevent blocking
            QueuedAction? queuedAction = null;
            lock (_queueLock)
            {
                if (_actionQueue.Count > 0)
                    queuedAction = _actionQueue.Dequeue();
            }
            
            if (queuedAction != null)
            {
                try
                {
                    queuedAction.Action.Invoke();
                    queuedAction.CompletionSource.SetResult(true);
                }
                catch (Exception ex)
                {
                    var errorMessage = queuedAction.AggregateId.HasValue 
                        ? $"Main thread action error for {queuedAction.EventTypeName} (ID: {queuedAction.AggregateId}): {ex.Message}"
                        : $"Main thread action error for {queuedAction.EventTypeName}: {ex.Message}";
                    EventBusLogger.LogError(errorMessage);
                    queuedAction.CompletionSource.SetException(ex);
                }
            }
        }

        public void Dispatch(Action? action, string? eventTypeName = null, Guid? aggregateId = null)
        {
            if (action == null) return;
            try
            {
                if (_mainThreadContext != null)
                {
                    // Use SynchronizationContext.Post to avoid blocking - fire and forget
                    _mainThreadContext.Post(_ => 
                    {
                        try
                        {
                            action.Invoke();
                        }
                        catch (Exception ex)
                        {
                            var errorMessage = aggregateId.HasValue 
                                ? $"Handler error for {eventTypeName} (ID: {aggregateId}): {ex.Message}"
                                : $"Handler error for {eventTypeName}: {ex.Message}";
                            EventBusLogger.LogError(errorMessage);
                        }
                    }, null);
                }
                else
                {
                    _mainThreadContext = SynchronizationContext.Current;
                }
            }
            catch (Exception ex)
            {
                var errorMessage = aggregateId.HasValue 
                    ? $"Dispatch error for {eventTypeName} (ID: {aggregateId}): {ex.Message}"
                    : $"Dispatch error for {eventTypeName}: {ex.Message}";
                EventBusLogger.LogError(errorMessage);
            }
        }

        public void DispatchAndWait(Action? action, string? eventTypeName = null, Guid? aggregateId = null)
        {
            if (action == null) return;
            try
            {
                if (SynchronizationContext.Current == _mainThreadContext)
                {
                    // Already on the main thread, execute immediately
                    action.Invoke();
                }
                else
                {
                    // On background thread, queue the action and wait for completion
                    // This maintains FIFO order by ensuring each action completes before the next
                    var queuedAction = new QueuedAction(action, eventTypeName, aggregateId);
                    
                    lock (_queueLock)
                    {
                        _actionQueue.Enqueue(queuedAction);
                    }
                    
                    // Wait for the action to complete on the main thread
                    // This ensures FIFO order - each event waits for the previous one to complete
                    queuedAction.CompletionSource.Task.Wait();
                }
            }
            catch (Exception ex)
            {
                var errorMessage = aggregateId.HasValue 
                    ? $"DispatchAndWait error for {eventTypeName} (ID: {aggregateId}): {ex.Message}"
                    : $"DispatchAndWait error for {eventTypeName}: {ex.Message}";
                EventBusLogger.LogError(errorMessage);
            }
        }

        /// <summary>
        /// Cleans up the UnityDispatcher resources.
        /// This can be called manually or is automatically called by Unity's OnDestroy.
        /// </summary>
        public void Cleanup()
        {
            // Complete all pending TaskCompletionSources to prevent hanging
            lock (_queueLock)
            {
                while (_actionQueue.Count > 0)
                {
                    var queuedAction = _actionQueue.Dequeue();
                    try
                    {
                        if (!queuedAction.CompletionSource.Task.IsCompleted)
                        {
                            queuedAction.CompletionSource.SetCanceled();
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        // Already disposed, ignore
                    }
                }
            }

            // Clear static references
            _instance = null;
            _mainThreadContext = null;
        }

        private void OnDestroy()
        {
            Cleanup();
        }

    }
}