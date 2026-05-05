#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace com.DvosTools.bus.Core
{
    internal interface IBucketOps
    {
        void ClearAll();
        /// <summary>Reset: drops handlers, queued events, and buffered events for this aggregate (all of T).</summary>
        void ResetAggregate(Guid aggregateId);
        /// <summary>Events-only: drops queued + buffered events for this aggregate; KEEPS handlers.</summary>
        void ClearEventsForAggregate(Guid aggregateId);
        int CountForAggregate(Guid aggregateId);
        void DrainBufferedFor(Guid aggregateId);
        int QueueCount();
        int BufferedCount(Guid aggregateId);
        int TotalBufferedCount();
        IEnumerable<Guid> BufferedAggregateIds();
        bool HasHandlerForAggregate(Guid aggregateId);
        void DrainBufferedForHandlers(Guid aggregateId, IReadOnlyCollection<Delegate> handlers);
    }

    internal static class EventBusRegistry
    {
        private static readonly ConcurrentDictionary<Type, IBucketOps> _ops = new();

        public static void Register<T>() => _ops.TryAdd(typeof(T), TypedBucketOps<T>.Instance);

        public static IEnumerable<IBucketOps> All => _ops.Values;

        public static int TotalQueueCount()
        {
            int total = 0;
            foreach (var op in _ops.Values) total += op.QueueCount();
            return total;
        }

        public static int CountForAggregate(Guid id)
        {
            int total = 0;
            foreach (var op in _ops.Values) total += op.CountForAggregate(id);
            return total;
        }

        public static int TotalBuffered()
        {
            int total = 0;
            foreach (var op in _ops.Values) total += op.TotalBufferedCount();
            return total;
        }

        public static int BufferedFor(Guid id)
        {
            int total = 0;
            foreach (var op in _ops.Values) total += op.BufferedCount(id);
            return total;
        }

        public static IEnumerable<Guid> BufferedAggregateIds()
        {
            var seen = new HashSet<Guid>();
            foreach (var op in _ops.Values)
                foreach (var id in op.BufferedAggregateIds())
                    seen.Add(id);
            return seen;
        }

        public static void ClearAllAcrossTypes()
        {
            foreach (var op in _ops.Values) op.ClearAll();
        }

        public static void ClearAggregateAcrossTypes(Guid id)
        {
            foreach (var op in _ops.Values) op.ResetAggregate(id);
        }

        public static void ClearEventsForAggregateAcrossTypes(Guid id)
        {
            foreach (var op in _ops.Values) op.ClearEventsForAggregate(id);
        }

        public static void DrainBufferedForAggregate(Guid id)
        {
            foreach (var op in _ops.Values) op.DrainBufferedFor(id);
        }

        public static void DrainBufferedForHandlersAcrossTypes(Guid id, IReadOnlyCollection<Delegate> handlers)
        {
            foreach (var op in _ops.Values) op.DrainBufferedForHandlers(id, handlers);
        }
    }

    internal sealed class TypedBucketOps<T> : IBucketOps
    {
        public static readonly TypedBucketOps<T> Instance = new();

        public void ClearAll()
        {
            lock (HandlerStore<T>.Lock)
            {
                HandlerStore<T>.ByAggregate.Clear();
                HandlerStore<T>.RoutedSnapshot.Clear();
                HandlerStore<T>.GlobalSnapshot = null;
            }
            lock (EventQueueStore<T>.Lock) EventQueueStore<T>.Queue.Clear();
            lock (BufferStore<T>.Lock) BufferStore<T>.ByAggregate.Clear();
        }

        public void ResetAggregate(Guid aggregateId)
        {
            if (aggregateId == Guid.Empty) return;
            lock (HandlerStore<T>.Lock)
            {
                if (HandlerStore<T>.ByAggregate.Remove(aggregateId))
                    HandlerStore<T>.RebuildRoutedSnapshot(aggregateId);
            }
            ClearEventsForAggregate(aggregateId);
        }

        public void ClearEventsForAggregate(Guid aggregateId)
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
            lock (BufferStore<T>.Lock) BufferStore<T>.ByAggregate.Remove(aggregateId);
        }

        public int CountForAggregate(Guid aggregateId)
        {
            lock (HandlerStore<T>.Lock)
            {
                return HandlerStore<T>.ByAggregate.TryGetValue(aggregateId, out var list) ? list.Count : 0;
            }
        }

        public void DrainBufferedFor(Guid aggregateId)
        {
            Queue<QueuedEvent<T>>? events;
            lock (BufferStore<T>.Lock)
            {
                BufferStore<T>.ByAggregate.TryGetValue(aggregateId, out events);
                BufferStore<T>.ByAggregate.Remove(aggregateId);
            }
            if (events == null) return;
            while (events.Count > 0)
            {
                var ev = events.Dequeue();
                EventBusService.DispatchInPlace(in ev);
            }
        }

        public void DrainBufferedForHandlers(Guid aggregateId, IReadOnlyCollection<Delegate> handlers)
        {
            Queue<QueuedEvent<T>>? eventsToDispatch = null;
            lock (BufferStore<T>.Lock)
            {
                if (!BufferStore<T>.ByAggregate.TryGetValue(aggregateId, out var bufferedQueue)) return;
                bool typeMatches = false;
                lock (HandlerStore<T>.Lock)
                {
                    if (HandlerStore<T>.ByAggregate.TryGetValue(aggregateId, out var list))
                    {
                        foreach (var sub in list)
                        {
                            foreach (var h in handlers)
                                if (ReferenceEquals(sub.OriginalHandler, h)) { typeMatches = true; break; }
                            if (typeMatches) break;
                        }
                    }
                }
                if (!typeMatches) return;
                eventsToDispatch = bufferedQueue;
                BufferStore<T>.ByAggregate.Remove(aggregateId);
            }
            while (eventsToDispatch.Count > 0)
            {
                var ev = eventsToDispatch.Dequeue();
                EventBusService.DispatchInPlace(in ev);
            }
        }

        public int QueueCount()
        {
            lock (EventQueueStore<T>.Lock) return EventQueueStore<T>.Queue.Count;
        }

        public int BufferedCount(Guid aggregateId)
        {
            lock (BufferStore<T>.Lock)
                return BufferStore<T>.ByAggregate.TryGetValue(aggregateId, out var q) ? q.Count : 0;
        }

        public int TotalBufferedCount()
        {
            int total = 0;
            lock (BufferStore<T>.Lock)
                foreach (var q in BufferStore<T>.ByAggregate.Values) total += q.Count;
            return total;
        }

        public IEnumerable<Guid> BufferedAggregateIds()
        {
            lock (BufferStore<T>.Lock)
            {
                var copy = new List<Guid>(BufferStore<T>.ByAggregate.Keys);
                return copy;
            }
        }

        public bool HasHandlerForAggregate(Guid aggregateId)
        {
            lock (HandlerStore<T>.Lock)
                return HandlerStore<T>.ByAggregate.TryGetValue(aggregateId, out var list) && list.Count > 0;
        }
    }
}
