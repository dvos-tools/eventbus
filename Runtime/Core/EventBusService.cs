#nullable enable
using System;
using System.Collections.Generic;
using com.DvosTools.bus.Dispatchers;

namespace com.DvosTools.bus.Core
{
    internal static class EventBusService
    {
        public static void Send<T>(in T eventData, Guid aggregateId = default)
        {
            EventBusRegistry.Register<T>();
            Guid id = aggregateId;
            if (id == Guid.Empty && RoutingProbe<T>.Extract != null)
                id = RoutingProbe<T>.Extract(eventData);

            if (id != Guid.Empty && !HasAnyHandler<T>(id))
            {
                Buffer(in eventData, id);
                EventBusLogger.Log($"Buffered {EventTypeName<T>.Value} for aggregate ID {id} (no handler available)");
                return;
            }

            var queued = new QueuedEvent<T> { Event = eventData, AggregateId = id, QueuedAt = DateTime.UtcNow };
            QueueScheduler.Enqueue(in queued);
            EventBusLogger.Log($"Queued {EventTypeName<T>.Value}");
        }

        public static void SendAndWait<T>(in T eventData, Guid aggregateId = default)
        {
            EventBusRegistry.Register<T>();
            Guid id = aggregateId;
            if (id == Guid.Empty && RoutingProbe<T>.Extract != null)
                id = RoutingProbe<T>.Extract(eventData);

            Subscription<T>[]? globals;
            Subscription<T>[]? routed = null;
            lock (HandlerStore<T>.Lock)
            {
                globals = HandlerStore<T>.GlobalSnapshot;
                if (id != Guid.Empty)
                    HandlerStore<T>.RoutedSnapshot.TryGetValue(id, out routed);
            }

            if (globals == null && routed == null)
            {
                EventBusLogger.LogWarning($"No handlers for {EventTypeName<T>.Value}");
                return;
            }

            Guid? logId = id == Guid.Empty ? (Guid?)null : id;
            if (globals != null)
                foreach (var sub in globals)
                    sub.Dispatcher.DispatchAndWait(sub.Handler, in eventData, EventTypeName<T>.Value, logId);
            if (routed != null)
                foreach (var sub in routed)
                    sub.Dispatcher.DispatchAndWait(sub.Handler, in eventData, EventTypeName<T>.Value, logId);
        }

        /// <summary>Synchronous in-place dispatch from buffered storage. Used by AggregateReady drain.</summary>
        public static void DispatchInPlace<T>(in QueuedEvent<T> ev)
        {
            Subscription<T>[]? globals;
            Subscription<T>[]? routed = null;
            lock (HandlerStore<T>.Lock)
            {
                globals = HandlerStore<T>.GlobalSnapshot;
                if (ev.AggregateId != Guid.Empty)
                    HandlerStore<T>.RoutedSnapshot.TryGetValue(ev.AggregateId, out routed);
            }
            if (globals == null && routed == null)
            {
                EventBusLogger.LogWarning($"No handlers for {EventTypeName<T>.Value}");
                return;
            }
            Guid? logId = ev.AggregateId == Guid.Empty ? (Guid?)null : ev.AggregateId;
            if (globals != null)
                foreach (var sub in globals)
                    sub.Dispatcher.DispatchAndWait(sub.Handler, in ev.Event, EventTypeName<T>.Value, logId);
            if (routed != null)
                foreach (var sub in routed)
                    sub.Dispatcher.DispatchAndWait(sub.Handler, in ev.Event, EventTypeName<T>.Value, logId);
        }

        public static void RegisterHandler<T>(Action<T> handler, Guid aggregateId = default, IDispatcher? dispatcher = null)
        {
            EventBusRegistry.Register<T>();
            // Default dispatcher kept as ThreadPoolDispatcher for backwards-compat with existing async tests.
            // Use RegisterUnityHandler explicitly to opt into main-thread dispatch.
            var dispatcherInstance = dispatcher ?? new ThreadPoolDispatcher();
            var sub = new Subscription<T>(handler, dispatcherInstance, aggregateId, handler);

            lock (HandlerStore<T>.Lock)
            {
                if (!HandlerStore<T>.ByAggregate.TryGetValue(aggregateId, out var list))
                {
                    list = new List<Subscription<T>>();
                    HandlerStore<T>.ByAggregate[aggregateId] = list;
                }
                list.Add(sub);
                HandlerStore<T>.RebuildRoutedSnapshot(aggregateId);
            }

            EventBusLogger.Log(aggregateId != Guid.Empty
                ? $"Registered routed handler for {EventTypeName<T>.Value} (ID: {aggregateId})"
                : $"Registered handler for {EventTypeName<T>.Value}");
        }

        public static void DisposeHandlers<T>()
        {
            lock (HandlerStore<T>.Lock)
            {
                HandlerStore<T>.ByAggregate.Clear();
                HandlerStore<T>.RoutedSnapshot.Clear();
                HandlerStore<T>.GlobalSnapshot = null;
            }
            lock (EventQueueStore<T>.Lock) EventQueueStore<T>.Queue.Clear();
        }

        public static void DisposeHandlersForAggregate<T>(Guid aggregateId)
        {
            if (aggregateId == Guid.Empty) return;
            lock (HandlerStore<T>.Lock)
            {
                if (HandlerStore<T>.ByAggregate.Remove(aggregateId))
                    HandlerStore<T>.RebuildRoutedSnapshot(aggregateId);
            }
            // Match legacy behavior: dropping handlers for this T at aggregate clears events for the
            // aggregate across all event types (their delivery target is gone for this aggregate scope).
            EventBusRegistry.ClearEventsForAggregateAcrossTypes(aggregateId);
        }

        public static void DisposeHandlerFromAggregate<T>(Action<T> handler, Guid aggregateId)
        {
            if (aggregateId == Guid.Empty) return;
            bool clearEvents = true;
            lock (HandlerStore<T>.Lock)
            {
                if (HandlerStore<T>.ByAggregate.TryGetValue(aggregateId, out var list))
                {
                    list.RemoveAll(s => s.OriginalHandler.Equals(handler));
                    if (list.Count == 0) HandlerStore<T>.ByAggregate.Remove(aggregateId);
                    HandlerStore<T>.RebuildRoutedSnapshot(aggregateId);
                }
            }
            foreach (var op in EventBusRegistry.All)
                if (op.HasHandlerForAggregate(aggregateId)) { clearEvents = false; break; }

            // Cross-type clear: drop queued + buffered events for this aggregate across all T's.
            if (clearEvents) EventBusRegistry.ClearEventsForAggregateAcrossTypes(aggregateId);
        }

        public static void AggregateReady(Guid aggregateId)
        {
            if (aggregateId == Guid.Empty)
            {
                EventBusLogger.LogWarning("Cannot mark empty aggregate ID as ready");
                return;
            }
            int total = EventBusRegistry.BufferedFor(aggregateId);
            if (total == 0)
            {
                EventBusLogger.Log($"No buffered events found for aggregate ID {aggregateId}");
                return;
            }
            EventBusRegistry.DrainBufferedForAggregate(aggregateId);
            EventBusLogger.Log($"Processed {total} buffered events immediately (aggregate ID: {aggregateId})");
        }

        public static void AggregateReady(Guid aggregateId, IReadOnlyCollection<Delegate> handlers)
        {
            if (aggregateId == Guid.Empty)
            {
                EventBusLogger.LogWarning("Cannot mark empty aggregate ID as ready");
                return;
            }
            if (handlers == null || handlers.Count == 0)
            {
                EventBusLogger.LogWarning("AggregateReady with an empty handler list does nothing; use AggregateReady(aggregateId) without handlers to flush the entire buffer.");
                return;
            }
            EventBusRegistry.DrainBufferedForHandlersAcrossTypes(aggregateId, handlers);
        }

        private static bool HasAnyHandler<T>(Guid aggregateId)
        {
            lock (HandlerStore<T>.Lock)
            {
                if (HandlerStore<T>.GlobalSnapshot != null) return true;
                return HandlerStore<T>.ByAggregate.TryGetValue(aggregateId, out var list) && list.Count > 0;
            }
        }

        private static void Buffer<T>(in T eventData, Guid aggregateId)
        {
            var queued = new QueuedEvent<T> { Event = eventData, AggregateId = aggregateId, QueuedAt = DateTime.UtcNow };
            lock (BufferStore<T>.Lock)
            {
                if (!BufferStore<T>.ByAggregate.TryGetValue(aggregateId, out var q))
                {
                    q = new Queue<QueuedEvent<T>>();
                    BufferStore<T>.ByAggregate[aggregateId] = q;
                }
                q.Enqueue(queued);
            }
        }

        private static void ClearEventsForAggregateInQueue<T>(Guid aggregateId)
        {
            lock (EventQueueStore<T>.Lock)
            {
                var q = EventQueueStore<T>.Queue;
                int count = q.Count;
                for (int i = 0; i < count; i++)
                {
                    var ev = q.Dequeue();
                    if (ev.AggregateId != aggregateId) q.Enqueue(ev);
                }
            }
        }
    }
}
