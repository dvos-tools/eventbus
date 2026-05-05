#nullable enable
using System;
using System.Collections.Generic;

namespace com.DvosTools.bus.Core
{
    /// <summary>
    /// Per-T handler storage. Static fields = JIT-inlined lookup, no Dictionary&lt;Type, ...&gt; probe.
    /// Guid.Empty bucket holds global handlers; other Guid keys hold routed handlers.
    /// GlobalSnapshot/RoutedSnapshot are cached arrays rebuilt on register/unregister so
    /// hot-path SendAndWait reads a stable array under lock with zero per-call allocation.
    /// </summary>
    internal static class HandlerStore<T>
    {
        public static readonly Dictionary<Guid, List<Subscription<T>>> ByAggregate = new();
        public static readonly object Lock = new();
        public static Subscription<T>[]? GlobalSnapshot;
        public static readonly Dictionary<Guid, Subscription<T>[]> RoutedSnapshot = new();

        // Caller MUST hold Lock.
        public static void RebuildGlobalSnapshot()
        {
            if (ByAggregate.TryGetValue(Guid.Empty, out var list) && list.Count > 0)
                GlobalSnapshot = list.ToArray();
            else
                GlobalSnapshot = null;
        }

        // Caller MUST hold Lock.
        public static void RebuildRoutedSnapshot(Guid aggregateId)
        {
            if (aggregateId == Guid.Empty) { RebuildGlobalSnapshot(); return; }
            if (ByAggregate.TryGetValue(aggregateId, out var list) && list.Count > 0)
                RoutedSnapshot[aggregateId] = list.ToArray();
            else
                RoutedSnapshot.Remove(aggregateId);
        }
    }
}
