using System;

namespace com.DvosTools.bus.Runtime
{
    /// <summary>
    /// Interface for events that support routing based on aggregate ID.
    /// Events implementing this interface can be routed to specific handlers
    /// that have subscribed with matching aggregate IDs.
    /// </summary>
    public interface IRoutableEvent
    {
        /// <summary>
        /// The aggregate ID used for routing this event to specific handlers.
        /// This should be a UUID that identifies the aggregate root.
        /// </summary>
        Guid AggregateId { get; }
    }
}