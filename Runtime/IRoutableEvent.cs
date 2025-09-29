using System;

namespace com.DvosTools.bus
{
    /// <summary>
    /// Interface for events that support routing based on aggregate ID.
    /// Events implementing this interface can be routed to specific handlers
    /// that have subscribed with matching aggregate IDs.
    /// </summary>
    public interface IRoutableEvent
    {
        Guid AggregateId { get; }
    }
}