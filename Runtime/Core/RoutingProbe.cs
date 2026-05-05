#nullable enable
using System;

namespace com.DvosTools.bus.Core
{
    internal static class EventTypeName<T>
    {
        public static readonly string Value = typeof(T).Name;
    }

    /// <summary>
    /// Auto-extracts AggregateId from class events implementing IRoutableEvent.
    /// Skipped for value types because boxing the struct to call the interface would defeat zero-alloc.
    /// Struct events must pass aggregateId explicitly to Send/SendAndWait.
    /// </summary>
    internal static class RoutingProbe<T>
    {
        public static readonly Func<T, Guid>? Extract;

        static RoutingProbe()
        {
            if (!typeof(T).IsValueType && typeof(IRoutableEvent).IsAssignableFrom(typeof(T)))
            {
                Extract = e => ((IRoutableEvent)e!).AggregateId;
            }
        }
    }
}
