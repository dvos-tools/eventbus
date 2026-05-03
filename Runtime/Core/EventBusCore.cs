#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace com.DvosTools.bus.Core
{
    internal class EventBusCore
    {
        private static EventBusCore? _instance;
        private static EventBusService? _service;

        // Event handlers indexed by event type, then by aggregate id.
        // Guid.Empty bucket holds global (non-routed) handlers.
        public readonly Dictionary<Type, Dictionary<Guid, List<Subscription>>> Handlers = new();
        public readonly Queue<QueuedEvent> EventQueue = new();
        public readonly Dictionary<Guid, Queue<QueuedEvent>> BufferedEvents = new();
        public readonly object QueueLock = new();
        public readonly object BufferedEventsLock = new();
        public readonly object HandlersLock = new();
        private CancellationTokenSource _cancellationTokenSource;
        private SemaphoreSlim _queueSignal = new(0);



        public static EventBusCore Instance => _instance ??= new EventBusCore();
        public static EventBusService Service => _service ??= new EventBusService(Instance);

        private EventBusCore()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _ = Task.Run(ProcessEventQueueAsync, _cancellationTokenSource.Token); // Start the background queue processor
        }

        /// <summary>Signal the worker that a new event has been enqueued.</summary>
        public void NotifyQueued()
        {
            try { _queueSignal.Release(); }
            catch (ObjectDisposedException) { /* shutting down */ }
            catch (SemaphoreFullException) { /* impossible without max-count, defensive */ }
        }

        private async Task ProcessEventQueueAsync()
        {
            var token = _cancellationTokenSource.Token;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await _queueSignal.WaitAsync(token);

                    QueuedEvent? eventToProcess = null;
                    lock (QueueLock)
                    {
                        if (EventQueue.Count > 0)
                            eventToProcess = EventQueue.Dequeue();
                    }

                    if (eventToProcess != null)
                        await Service.ProcessEventAsync(eventToProcess);
                    // else: stale signal (e.g. ClearAll drained the queue). Loop back to wait.
                }
                catch (OperationCanceledException)
                {
                    break; // Expected when cancellation is requested
                }
                catch (ObjectDisposedException)
                {
                    break; // Signal disposed during teardown
                }
                catch (Exception ex)
                {
                    // If cancellation has been requested, the exception is almost certainly
                    // shutdown-related (race with field replacement). Suppress to avoid spurious
                    // error logs failing tests that watch for unhandled error logs.
                    if (token.IsCancellationRequested) break;
                    EventBusLogger.LogError($"Queue processor error: {ex.Message}");
                }
            }
        }

        public int GetBufferedEventCount(Guid aggregateId)
        {
            lock (BufferedEventsLock)
            {
                return BufferedEvents.TryGetValue(aggregateId, out var bufferedQueue) ? bufferedQueue.Count : 0;
            }
        }

        public IEnumerable<Guid> GetBufferedAggregateIds()
        {
            lock (BufferedEventsLock)
            {
                return BufferedEvents.Keys.ToList();
            }
        }

        public int GetTotalBufferedEventCount()
        {
            lock (BufferedEventsLock)
            {
                return BufferedEvents.Values.Sum(queue => queue.Count);
            }
        }

        public void Shutdown()
        {
            try
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                }
            }
            catch (ObjectDisposedException)
            {
                // Already disposed, ignore
            }
        }

        public void ClearAll()
        {
            // Clear handlers
            lock (HandlersLock)
            {
                Handlers.Clear();
            }

            // Clear buffered events
            lock (BufferedEventsLock)
            {
                BufferedEvents.Clear();
            }

            // Clear event queue
            lock (QueueLock)
            {
                EventQueue.Clear();
            }

            // Reset the background task to ensure it's running properly
            ResetBackgroundTask();
        }

        public void ResetBackgroundTask()
        {
            // Cancel the current background task. The old worker will exit on the next loop
            // iteration via OperationCanceledException from WaitAsync(oldToken).
            Shutdown();

            // Recreate the CTS for the new worker. The semaphore is kept across resets — disposing
            // it here races with the old worker still parked in WaitAsync (it would throw an
            // unexpected ObjectDisposedException). Stale releases on the shared semaphore are
            // harmless: the next worker will wake, find an empty queue, and loop back to wait.
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            _ = Task.Run(ProcessEventQueueAsync, _cancellationTokenSource.Token);
        }

        public void DisposeHandlers<T>() where T : class
        {
            var eventType = typeof(T);
            lock (HandlersLock)
            {
                Handlers.Remove(eventType);
            }
            // Clear all queued events for this event type (alloc-free filter)
            lock (QueueLock)
            {
                int count = EventQueue.Count;
                for (int i = 0; i < count; i++)
                {
                    var queuedEvent = EventQueue.Dequeue();
                    if (queuedEvent.EventType != eventType)
                        EventQueue.Enqueue(queuedEvent);
                }
            }
        }

        public void DisposeAllHandlers()
        {
            lock (HandlersLock)
            {
                Handlers.Clear();
            }
            // Clear all queued events
            lock (QueueLock)
            {
                EventQueue.Clear();
            }
            // Clear all buffered events
            lock (BufferedEventsLock)
            {
                BufferedEvents.Clear();
            }
        }

        public void ClearEventsForAggregate(Guid aggregateId)
        {

            // Clear queued events for this aggregate (alloc-free filter via QueuedEvent.AggregateId field)
            lock (QueueLock)
            {
                int count = EventQueue.Count;
                for (int i = 0; i < count; i++)
                {
                    var queuedEvent = EventQueue.Dequeue();
                    if (queuedEvent.AggregateId != aggregateId)
                        EventQueue.Enqueue(queuedEvent);
                }
            }

            // Clear buffered events for this aggregate
            lock (BufferedEventsLock)
            {
                BufferedEvents.Remove(aggregateId);
            }
        }

        public void ResetAggregate(Guid aggregateId)
        {
            if (aggregateId == Guid.Empty) return;
            lock (HandlersLock)
            {
                foreach (var eventType in Handlers.Keys.ToList())
                {
                    var byAggregate = Handlers[eventType];
                    byAggregate.Remove(aggregateId);
                    if (byAggregate.Count == 0) Handlers.Remove(eventType);
                }
            }
            ClearEventsForAggregate(aggregateId);
        }

        public void DisposeHandlersForAggregate<T>(Guid aggregateId) where T : class
        {
            if (aggregateId == Guid.Empty) return;
            var eventType = typeof(T);
            lock (HandlersLock)
            {
                if (Handlers.TryGetValue(eventType, out var byAggregate))
                {
                    byAggregate.Remove(aggregateId);
                    if (byAggregate.Count == 0) Handlers.Remove(eventType);
                }
            }
            ClearEventsForAggregate(aggregateId);
        }

        public void DisposeHandlerFromAggregate<T>(Action<T> handler, Guid aggregateId) where T : class
        {
            if (aggregateId == Guid.Empty) return;

            bool hasRemainingHandlers = false;
            lock (HandlersLock)
            {
                var eventType = typeof(T);
                if (Handlers.TryGetValue(eventType, out var byAggregate))
                {
                    if (byAggregate.TryGetValue(aggregateId, out var list))
                    {
                        list.RemoveAll(sub => ReferenceEquals(sub.OriginalHandler, handler));
                        if (list.Count == 0) byAggregate.Remove(aggregateId);
                    }
                    if (byAggregate.Count == 0)
                    {
                        Handlers.Remove(eventType);
                    }
                }

                // Check if there are any remaining handlers for this aggregate across all event types
                foreach (var byAgg in Handlers.Values)
                {
                    if (byAgg.TryGetValue(aggregateId, out var list) && list.Count > 0)
                    {
                        hasRemainingHandlers = true;
                        break;
                    }
                }
            }

            // Only clear events if there are no remaining handlers for this aggregate
            if (!hasRemainingHandlers)
            {
                ClearEventsForAggregate(aggregateId);
            }
        }

        public int GetHandlerCountForAggregate(Guid aggregateId)
        {
            lock (HandlersLock)
            {
                int total = 0;
                foreach (var byAgg in Handlers.Values)
                {
                    if (byAgg.TryGetValue(aggregateId, out var list))
                        total += list.Count;
                }
                return total;
            }
        }

        public bool HasHandlersForAggregate(Guid aggregateId)
        {
            return GetHandlerCountForAggregate(aggregateId) > 0;
        }

        public int GetHandlerCount(Type eventType)
        {
            lock (HandlersLock)
            {
                if (!Handlers.TryGetValue(eventType, out var byAggregate)) return 0;
                int total = 0;
                foreach (var list in byAggregate.Values) total += list.Count;
                return total;
            }
        }

        /// <summary>
        /// Builds a flat snapshot of (eventType -> all subscriptions) for the deprecated public API.
        /// </summary>
        public Dictionary<Type, List<Subscription>> SnapshotFlatHandlers()
        {
            lock (HandlersLock)
            {
                var snapshot = new Dictionary<Type, List<Subscription>>(Handlers.Count);
                foreach (var (eventType, byAggregate) in Handlers)
                {
                    var flat = new List<Subscription>();
                    foreach (var list in byAggregate.Values) flat.AddRange(list);
                    snapshot[eventType] = flat;
                }
                return snapshot;
            }
        }

        public void Dispose()
        {
            // Cancel the background task
            Shutdown();

            // Dispose the cancellation token source and signal
            _cancellationTokenSource.Dispose();
            try { _queueSignal.Dispose(); } catch (ObjectDisposedException) { /* already */ }

            // Clear all collections to free memory
            lock (HandlersLock)
            {
                Handlers.Clear();
            }

            lock (BufferedEventsLock)
            {
                BufferedEvents.Clear();
            }

            lock (QueueLock)
            {
                EventQueue.Clear();
            }
        }

    }
}
