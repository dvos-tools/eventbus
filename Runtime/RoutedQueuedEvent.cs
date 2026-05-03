using System;

namespace com.DvosTools.bus
{
    /// <summary>
    /// [DEPRECATED] AggregateId is now stored directly on <see cref="QueuedEvent"/>.
    /// This wrapper is retained for backwards compatibility only.
    /// </summary>
    [Obsolete("Read QueuedEvent.AggregateId directly. RoutedQueuedEvent will be removed in a future version.")]
    public class RoutedQueuedEvent : QueuedEvent
    {
        public RoutedQueuedEvent(
            object eventData,
            Type eventType,
            DateTime queuedAt = default,
            Guid aggregateId = default
        ) : base(eventData, eventType, queuedAt)
        {
            AggregateId = aggregateId;
        }

        public static RoutedQueuedEvent FromQueuedEvent(QueuedEvent queuedEvent)
        {
            return new RoutedQueuedEvent(
                queuedEvent.EventData,
                queuedEvent.EventType,
                queuedEvent.QueuedAt,
                queuedEvent.AggregateId
            );
        }
    }
}
