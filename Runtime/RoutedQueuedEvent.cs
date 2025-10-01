using System;

namespace com.DvosTools.bus
{
    /// <summary>
    /// Represents a queued event with routing information.
    /// This extends the basic QueuedEvent with aggregate ID for routing.
    /// </summary>
    public class RoutedQueuedEvent : QueuedEvent
    {
        /// <summary>
        /// The aggregate ID for routing this event.
        /// Guid.Empty if the event doesn't support routing.
        /// </summary>
        public Guid AggregateId { get; set; }

        public RoutedQueuedEvent(
            object eventData,
            Type eventType,
            DateTime queuedAt = default,
            Guid aggregateId = default
        ) : base(eventData, eventType, queuedAt)
        {
            AggregateId = aggregateId;
        }

        /// <summary>
        /// Creates a RoutedQueuedEvent from a regular QueuedEvent.
        /// If the event implements IRoutableEvent, extract the aggregate ID.
        /// </summary>
        public static RoutedQueuedEvent FromQueuedEvent(QueuedEvent queuedEvent)
        {
            var aggregateId = queuedEvent.EventData is IRoutableEvent routableEvent 
                ? routableEvent.AggregateId 
                : Guid.Empty;

            return new RoutedQueuedEvent(
                queuedEvent.EventData,
                queuedEvent.EventType,
                queuedEvent.QueuedAt,
                aggregateId
            );
        }
    }
}