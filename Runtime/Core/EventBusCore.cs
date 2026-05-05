#nullable enable
using System;
using System.Collections.Generic;

namespace com.DvosTools.bus.Core
{
    /// <summary>
    /// Thin facade over EventBusRegistry + QueueScheduler. The previous singleton with shared
    /// Handlers / EventQueue / BufferedEvents dictionaries is replaced by per-T static stores.
    /// This class exists for the public API in EventBus.cs and the deprecated EventBusInstance shim.
    /// </summary>
    internal static class EventBusCore
    {
        static EventBusCore() { QueueScheduler.Start(); }

        public static int GetBufferedEventCount(Guid aggregateId) => EventBusRegistry.BufferedFor(aggregateId);

        public static IEnumerable<Guid> GetBufferedAggregateIds() => EventBusRegistry.BufferedAggregateIds();

        public static int GetTotalBufferedEventCount() => EventBusRegistry.TotalBuffered();

        public static int GetTotalQueueCount() => EventBusRegistry.TotalQueueCount();

        public static int GetHandlerCountForAggregate(Guid id) => EventBusRegistry.CountForAggregate(id);

        public static bool HasHandlersForAggregate(Guid id) => EventBusRegistry.CountForAggregate(id) > 0;

        public static int GetHandlerCount<T>()
        {
            lock (HandlerStore<T>.Lock)
            {
                int total = 0;
                foreach (var list in HandlerStore<T>.ByAggregate.Values) total += list.Count;
                return total;
            }
        }

        public static void ClearAll()
        {
            EventBusRegistry.ClearAllAcrossTypes();
            QueueScheduler.ClearReady();
            QueueScheduler.Reset();
        }

        public static void Shutdown() => QueueScheduler.Shutdown();

        public static void Dispose()
        {
            QueueScheduler.Shutdown();
            EventBusRegistry.ClearAllAcrossTypes();
            QueueScheduler.ClearReady();
        }

        public static void ClearEventsForAggregate(Guid id)
        {
            // Events-only: keep handlers, drop queued + buffered events for this aggregate.
            EventBusRegistry.ClearEventsForAggregateAcrossTypes(id);
            // Note: ready-list still holds drainable refs that point at the now-emptier per-T queues.
            // Drainables are stateless; if they wake against an empty queue they no-op. Safe to leave.
        }

        public static void ResetAggregate(Guid id)
        {
            if (id == Guid.Empty) return;
            EventBusRegistry.ClearAggregateAcrossTypes(id);
        }
    }
}
