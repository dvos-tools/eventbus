#nullable enable
using System.Collections.Generic;

namespace com.DvosTools.bus.Core
{
    internal static class EventQueueStore<T>
    {
        public static readonly Queue<QueuedEvent<T>> Queue = new();
        public static readonly object Lock = new();
    }
}
