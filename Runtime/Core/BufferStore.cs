#nullable enable
using System;
using System.Collections.Generic;

namespace com.DvosTools.bus.Core
{
    internal static class BufferStore<T>
    {
        public static readonly Dictionary<Guid, Queue<QueuedEvent<T>>> ByAggregate = new();
        public static readonly object Lock = new();
    }
}
