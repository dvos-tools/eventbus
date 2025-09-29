using System;

namespace com.DvosTools.bus
{
    public class QueuedEvent
    {
        public object EventData;
        public Type EventType;
        public DateTime QueuedAt;

        public QueuedEvent(
            object eventData,
            Type eventType,
            DateTime queuedAt = default
        )
        {
            EventType = eventType;
            EventData = eventData;
            QueuedAt = queuedAt == default ? DateTime.UtcNow : queuedAt;
        }
    }
}