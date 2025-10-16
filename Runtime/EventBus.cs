#nullable enable
using System;
using System.Collections.Generic;
using com.DvosTools.bus.Core;
using com.DvosTools.bus.Dispatchers;

namespace com.DvosTools.bus
{
    /// <summary>
    /// Main static API for the Event Bus system.
    /// Provides a clean, easy-to-use interface for event publishing, subscription, and management.
    /// </summary>
    public static class EventBus
    {
        private static readonly EventBusCore CoreEventBus = EventBusCore.Instance;

        /// <summary>
        /// Gets the EventBus instance. This is provided for backwards compatibility.
        /// Prefer using the static methods directly (e.g., EventBus.Send() instead of EventBus.Instance.Send()).
        /// </summary>
        public static EventBusInstance Instance => new EventBusInstance();

        // ===== CORE API =====

        /// <summary>
        /// Sends an event asynchronously. The event will be queued and processed by registered handlers.
        /// </summary>
        /// <typeparam name="T">The type of event to send</typeparam>
        /// <param name="eventData">The event data to send</param>
        public static void Send<T>(T eventData) where T : class
        {
            CoreEventBus.Send(eventData);
        }

        /// <summary>
        /// Sends an event synchronously and waits for all handlers to complete.
        /// Use this when you need to ensure the event is fully processed before continuing.
        /// </summary>
        /// <typeparam name="T">The type of event to send</typeparam>
        /// <param name="eventData">The event data to send</param>
        public static void SendAndWait<T>(T eventData) where T : class
        {
            CoreEventBus.SendAndWait(eventData);
        }

        /// <summary>
        /// Registers a handler for a specific event type.
        /// </summary>
        /// <typeparam name="T">The type of event to handle</typeparam>
        /// <param name="handler">The handler function to execute when the event is received</param>
        /// <param name="aggregateId">Optional aggregate ID for routed events. Use Guid.Empty for global handlers</param>
        /// <param name="dispatcher">Optional dispatcher for handling the event. Uses ThreadPoolDispatcher by default</param>
        public static void RegisterHandler<T>(Action<T> handler, Guid aggregateId = default, IDispatcher? dispatcher = null) where T : class
        {
            EventBusCore.RegisterHandler(handler, aggregateId, dispatcher);
        }

        /// <summary>
        /// Unregisters all handlers for a specific event type.
        /// </summary>
        /// <typeparam name="T">The type of event to unregister</typeparam>
        public static void UnregisterHandlers<T>() where T : class
        {
            var eventType = typeof(T);
            if (CoreEventBus.Handlers.TryGetValue(eventType, out var handler))
            {
                handler.Clear();
            }
        }

        /// <summary>
        /// Unregisters all handlers for all event types.
        /// </summary>
        public static void UnregisterAllHandlers()
        {
            CoreEventBus.Handlers.Clear();
        }

        /// <summary>
        /// Marks an aggregate as ready, processing any buffered events for that aggregate.
        /// </summary>
        /// <param name="aggregateId">The aggregate ID to mark as ready</param>
        public static void AggregateReady(Guid aggregateId)
        {
            CoreEventBus.AggregateReady(aggregateId);
        }

        /// <summary>
        /// Gets the number of buffered events for a specific aggregate.
        /// </summary>
        /// <param name="aggregateId">The aggregate ID to check</param>
        /// <returns>The number of buffered events</returns>
        public static int GetBufferedEventCount(Guid aggregateId)
        {
            return CoreEventBus.GetBufferedEventCount(aggregateId);
        }

        /// <summary>
        /// Gets all aggregate IDs that have buffered events.
        /// </summary>
        /// <returns>Collection of aggregate IDs with buffered events</returns>
        public static IEnumerable<Guid> GetBufferedAggregateIds()
        {
            return CoreEventBus.GetBufferedAggregateIds();
        }

        /// <summary>
        /// Gets the total number of buffered events across all aggregates.
        /// </summary>
        /// <returns>Total number of buffered events</returns>
        public static int GetTotalBufferedEventCount()
        {
            return CoreEventBus.GetTotalBufferedEventCount();
        }

        /// <summary>
        /// Gets the number of events currently in the processing queue.
        /// </summary>
        /// <returns>Number of events in the queue</returns>
        public static int GetQueueCount()
        {
            lock (CoreEventBus.QueueLock)
            {
                return CoreEventBus.EventQueue.Count;
            }
        }

        /// <summary>
        /// Gets the number of registered handlers for a specific event type.
        /// </summary>
        /// <typeparam name="T">The type of event to check</typeparam>
        /// <returns>Number of registered handlers</returns>
        public static int GetHandlerCount<T>() where T : class
        {
            var eventType = typeof(T);
            return CoreEventBus.Handlers.TryGetValue(eventType, out var handlers) ? handlers.Count : 0;
        }

        /// <summary>
        /// Checks if there are any handlers registered for a specific event type.
        /// </summary>
        /// <typeparam name="T">The type of event to check</typeparam>
        /// <returns>True if handlers are registered, false otherwise</returns>
        public static bool HasHandlers<T>() where T : class
        {
            return GetHandlerCount<T>() > 0;
        }

        /// <summary>
        /// Shuts down the event bus, cancelling background processing.
        /// </summary>
        public static void Shutdown()
        {
            CoreEventBus.Shutdown();
        }

        // ===== CONVENIENCE METHODS =====

        /// <summary>
        /// Registers a handler that will be called on the Unity main thread.
        /// </summary>
        /// <typeparam name="T">The type of event to handle</typeparam>
        /// <param name="handler">The handler function</param>
        /// <param name="aggregateId">Optional aggregate ID for routed events</param>
        public static void RegisterUnityHandler<T>(Action<T> handler, Guid aggregateId = default) where T : class
        {
            RegisterHandler(handler, aggregateId, UnityDispatcher.Instance);
        }

        /// <summary>
        /// Registers a handler that will be called immediately on the current thread.
        /// </summary>
        /// <typeparam name="T">The type of event to handle</typeparam>
        /// <param name="handler">The handler function</param>
        /// <param name="aggregateId">Optional aggregate ID for routed events</param>
        public static void RegisterImmediateHandler<T>(Action<T> handler, Guid aggregateId = default) where T : class
        {
            RegisterHandler(handler, aggregateId, new ImmediateDispatcher());
        }

        /// <summary>
        /// Registers a handler that will be called on a background thread pool thread.
        /// </summary>
        /// <typeparam name="T">The type of event to handle</typeparam>
        /// <param name="handler">The handler function</param>
        /// <param name="aggregateId">Optional aggregate ID for routed events</param>
        public static void RegisterBackgroundHandler<T>(Action<T> handler, Guid aggregateId = default) where T : class
        {
            RegisterHandler(handler, aggregateId, new ThreadPoolDispatcher());
        }

        /// <summary>
        /// Registers a handler with a specific aggregate ID for routed events.
        /// This is a convenience method for the common case of routing events.
        /// </summary>
        /// <typeparam name="T">The type of event to handle</typeparam>
        /// <param name="handler">The handler function</param>
        /// <param name="aggregateId">The aggregate ID for routing</param>
        /// <param name="dispatcher">Optional dispatcher for handling the event</param>
        public static void RegisterRoutedHandler<T>(Action<T> handler, Guid aggregateId, IDispatcher? dispatcher = null) where T : class
        {
            if (aggregateId == Guid.Empty)
                throw new ArgumentException("Aggregate ID cannot be empty for routed handlers", nameof(aggregateId));
                
            RegisterHandler(handler, aggregateId, dispatcher);
        }

        /// <summary>
        /// Registers a global handler that will receive all events of the specified type.
        /// This is a convenience method for non-routed events.
        /// </summary>
        /// <typeparam name="T">The type of event to handle</typeparam>
        /// <param name="handler">The handler function</param>
        /// <param name="dispatcher">Optional dispatcher for handling the event</param>
        public static void RegisterGlobalHandler<T>(Action<T> handler, IDispatcher? dispatcher = null) where T : class
        {
            RegisterHandler(handler, Guid.Empty, dispatcher);
        }

        /// <summary>
        /// Checks if there are any buffered events for any aggregate.
        /// </summary>
        /// <returns>True if there are any buffered events, false otherwise</returns>
        public static bool HasBufferedEvents()
        {
            return GetTotalBufferedEventCount() > 0;
        }

        /// <summary>
        /// Checks if there are any events currently in the processing queue.
        /// </summary>
        /// <returns>True if there are events in the queue, false otherwise</returns>
        public static bool HasQueuedEvents()
        {
            return GetQueueCount() > 0;
        }
    }

    /// <summary>
    /// Wrapper class for backwards compatibility with the instance API.
    /// </summary>
    public class EventBusInstance
    {
        /// <summary>
        /// Sends an event asynchronously. The event will be queued and processed by registered handlers.
        /// </summary>
        /// <typeparam name="T">The type of event to send</typeparam>
        /// <param name="eventData">The event data to send</param>
        [Obsolete("Use EventBus.Send() instead of EventBus.Instance.Send(). The instance API is deprecated.")]
        public void Send<T>(T eventData) where T : class
        {
            EventBus.Send(eventData);
        }

        /// <summary>
        /// Sends an event synchronously and waits for all handlers to complete.
        /// Use this when you need to ensure the event is fully processed before continuing.
        /// </summary>
        /// <typeparam name="T">The type of event to send</typeparam>
        /// <param name="eventData">The event data to send</param>
        [Obsolete("Use EventBus.SendAndWait() instead of EventBus.Instance.SendAndWait(). The instance API is deprecated.")]
        public void SendAndWait<T>(T eventData) where T : class
        {
            EventBus.SendAndWait(eventData);
        }

        /// <summary>
        /// Marks an aggregate as ready, processing any buffered events for that aggregate.
        /// </summary>
        /// <param name="aggregateId">The aggregate ID to mark as ready</param>
        [Obsolete("Use EventBus.AggregateReady() instead of EventBus.Instance.AggregateReady(). The instance API is deprecated.")]
        public void AggregateReady(Guid aggregateId)
        {
            EventBus.AggregateReady(aggregateId);
        }

        /// <summary>
        /// Gets the number of buffered events for a specific aggregate.
        /// </summary>
        /// <param name="aggregateId">The aggregate ID to check</param>
        /// <returns>The number of buffered events</returns>
        [Obsolete("Use EventBus.GetBufferedEventCount() instead of EventBus.Instance.GetBufferedEventCount(). The instance API is deprecated.")]
        public int GetBufferedEventCount(Guid aggregateId)
        {
            return EventBus.GetBufferedEventCount(aggregateId);
        }

        /// <summary>
        /// Gets all aggregate IDs that have buffered events.
        /// </summary>
        /// <returns>Collection of aggregate IDs with buffered events</returns>
        [Obsolete("Use EventBus.GetBufferedAggregateIds() instead of EventBus.Instance.GetBufferedAggregateIds(). The instance API is deprecated.")]
        public IEnumerable<Guid> GetBufferedAggregateIds()
        {
            return EventBus.GetBufferedAggregateIds();
        }

        /// <summary>
        /// Gets the total number of buffered events across all aggregates.
        /// </summary>
        /// <returns>Total number of buffered events</returns>
        [Obsolete("Use EventBus.GetTotalBufferedEventCount() instead of EventBus.Instance.GetTotalBufferedEventCount(). The instance API is deprecated.")]
        public int GetTotalBufferedEventCount()
        {
            return EventBus.GetTotalBufferedEventCount();
        }

        /// <summary>
        /// Gets the number of events currently in the processing queue.
        /// </summary>
        /// <returns>Number of events in the queue</returns>
        [Obsolete("Use EventBus.GetQueueCount() instead of EventBus.Instance.GetQueueCount(). The instance API is deprecated.")]
        public int GetQueueCount()
        {
            return EventBus.GetQueueCount();
        }

        /// <summary>
        /// Gets the number of registered handlers for a specific event type.
        /// </summary>
        /// <typeparam name="T">The type of event to check</typeparam>
        /// <returns>Number of registered handlers</returns>
        [Obsolete("Use EventBus.GetHandlerCount() instead of EventBus.Instance.GetHandlerCount(). The instance API is deprecated.")]
        public int GetHandlerCount<T>() where T : class
        {
            return EventBus.GetHandlerCount<T>();
        }

        /// <summary>
        /// Checks if there are any handlers registered for a specific event type.
        /// </summary>
        /// <typeparam name="T">The type of event to check</typeparam>
        /// <returns>True if handlers are registered, false otherwise</returns>
        [Obsolete("Use EventBus.HasHandlers() instead of EventBus.Instance.HasHandlers(). The instance API is deprecated.")]
        public bool HasHandlers<T>() where T : class
        {
            return EventBus.HasHandlers<T>();
        }

        /// <summary>
        /// Shuts down the event bus, cancelling background processing.
        /// </summary>
        [Obsolete("Use EventBus.Shutdown() instead of EventBus.Instance.Shutdown(). The instance API is deprecated.")]
        public void Shutdown()
        {
            EventBus.Shutdown();
        }

        /// <summary>
        /// Direct access to handlers for backwards compatibility.
        /// Consider using GetHandlerCount() and HasHandlers() instead.
        /// </summary>
        [Obsolete("Direct access to Handlers is deprecated. Use EventBus.GetHandlerCount() and EventBus.HasHandlers() instead.")]
        public Dictionary<Type, List<Subscription>> Handlers => EventBusCore.Instance.Handlers;

        /// <summary>
        /// Direct access to even queue for backwards compatibility.
        /// Consider using GetQueueCount() and HasQueuedEvents() instead.
        /// </summary>
        [Obsolete("Direct access to EventQueue is deprecated. Use EventBus.GetQueueCount() and EventBus.HasQueuedEvents() instead.")]
        public Queue<QueuedEvent> EventQueue => EventBusCore.Instance.EventQueue;

        /// <summary>
        /// Direct access to queue lock for backwards compatibility.
        /// Consider using the static API methods instead.
        /// </summary>
        [Obsolete("Direct access to QueueLock is deprecated. Use the static API methods instead.")]
        public object QueueLock => EventBusCore.Instance.QueueLock;
    }
}