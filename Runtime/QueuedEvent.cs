using System;

namespace com.DvosTools.bus
{
    /// <summary>[Obsolete] Internal queue uses QueuedEvent&lt;T&gt; struct post-refactor.</summary>
    [Obsolete("QueuedEvent is internal post-refactor. Reading EventBusInstance.EventQueue returns an empty queue.")]
    public class QueuedEvent
    {
        public object EventData;
        public Type EventType;
        public DateTime QueuedAt;
        public Guid AggregateId;

        public QueuedEvent(object eventData, Type eventType, DateTime queuedAt = default)
        {
            EventType = eventType;
            EventData = eventData;
            QueuedAt = queuedAt == default ? DateTime.UtcNow : queuedAt;
            AggregateId = eventData is IRoutableEvent r ? r.AggregateId : Guid.Empty;
        }
    }
}
