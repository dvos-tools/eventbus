#nullable enable
using System;

namespace com.DvosTools.bus.Core
{
    internal struct QueuedEvent<T>
    {
        public T Event;
        public Guid AggregateId;
        public DateTime QueuedAt;
    }
}
