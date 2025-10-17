#nullable enable
using System;
using System.Collections.Generic;
using com.DvosTools.bus.Core;
using com.DvosTools.bus.Dispatchers;

namespace com.DvosTools.bus
{
    /// <summary>
    /// Central event communication hub for Unity applications.
    /// 
    /// The EventBus enables loose coupling between game systems by allowing any component to publish events
    /// that other components can subscribe to. Events can be sent immediately or buffered until specific
    /// game objects (aggregates) are ready to process them.
    /// 
    /// Key capabilities:
    /// - Publish events that any number of subscribers can receive
    /// - Route events to specific game objects using aggregate IDs
    /// - Buffer events until game objects are ready to handle them
    /// - Execute event handlers on different threads (main thread, background, or immediate)
    /// - Maintain event order and ensure thread safety
    /// 
    /// This is the primary interface for all event communication in your Unity project.
    /// </summary>
    public static class EventBus
    {
        private static readonly EventBusCore CoreEventBus = EventBusCore.Instance;

        /// <summary>
        /// Gets the EventBus instance. This is provided for backwards compatibility.
        /// Prefer using the static methods directly (e.g., EventBus.Send() instead of EventBus.Instance.Send()).
        /// </summary>
        [Obsolete("The instance API is deprecated.")]
        public static EventBusInstance Instance => new EventBusInstance();

        // ===== CORE API =====

        /// <summary>
        /// Publishes an event to all registered subscribers.
        /// 
        /// This method queues the event for processing and returns immediately. All handlers
        /// registered for this event type will be notified asynchronously. For routed events
        /// (implementing IRoutableEvent), only handlers registered for the specific aggregate
        /// will receive the event.
        /// 
        /// Use this for fire-and-forget event publishing where you don't need to wait for
        /// handlers to complete.
        /// </summary>
        /// <typeparam name="T">The type of event to send</typeparam>
        /// <param name="eventData">The event data to send</param>
        public static void Send<T>(T eventData) where T : class
        {
            CoreEventBus.Send(eventData);
        }

        /// <summary>
        /// Publishes an event and waits for all handlers to complete processing.
        /// 
        /// This method blocks the current thread until all registered handlers have finished
        /// executing. This is useful when you need to ensure that all side effects of an event
        /// have been processed before continuing with your code.
        /// 
        /// Use this for critical events where you need guaranteed processing order or when
        /// the next operation depends on the event being fully handled.
        /// </summary>
        /// <typeparam name="T">The type of event to send</typeparam>
        /// <param name="eventData">The event data to send</param>
        public static void SendAndWait<T>(T eventData) where T : class
        {
            CoreEventBus.SendAndWait(eventData);
        }

        /// <summary>
        /// Subscribes to receive events of a specific type.
        /// 
        /// When an event of the specified type is published, your handler function will be called.
        /// You can register multiple handlers for the same event type - they will all be notified.
        /// 
        /// For routed events (implementing IRoutableEvent), specify an aggregateId to only
        /// receive events for that specific game object. Use Guid.Empty for global handlers
        /// that receive all events of this type regardless of routing.
        /// 
        /// The dispatcher determines which thread your handler runs on.
        /// </summary>
        /// <typeparam name="T">The type of event to handle</typeparam>
        /// <param name="handler">The function to call when this event is received</param>
        /// <param name="aggregateId">Game object ID for routed events, or Guid.Empty for global handlers</param>
        /// <param name="dispatcher">Which thread to run the handler on (defaults to background thread)</param>
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
        /// Clears all handlers, buffered events, and queued events.
        /// This is useful for test cleanup.
        /// </summary>
        public static void ClearAll()
        {
            CoreEventBus.ClearAll();
        }

        /// <summary>
        /// Signals that a game object is ready to process its buffered events.
        /// 
        /// When you send routed events to game objects that aren't ready yet, those events
        /// get buffered. Call this method when the game object is initialized and ready to
        /// handle events. All buffered events for this aggregate will be processed immediately.
        /// 
        /// This is essential for proper event ordering - events sent before an object is
        /// ready will be processed in the correct order once it becomes ready.
        /// </summary>
        /// <param name="aggregateId">The game object ID that is now ready to process events</param>
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
        /// Subscribes to events with handlers that run on Unity's main thread.
        /// 
        /// Use this when your handler needs to access Unity APIs (GameObjects, Components, etc.)
        /// or when you need to update the UI. All Unity API calls must happen on the main thread.
        /// 
        /// This is the most common choice for game logic handlers that interact with Unity objects.
        /// </summary>
        /// <typeparam name="T">The type of event to handle</typeparam>
        /// <param name="handler">The function to call when this event is received</param>
        /// <param name="aggregateId">Game object ID for routed events, or Guid.Empty for global handlers</param>
        public static void RegisterUnityHandler<T>(Action<T> handler, Guid aggregateId = default) where T : class
        {
            RegisterHandler(handler, aggregateId, UnityDispatcher.Instance);
        }

        /// <summary>
        /// Subscribes to events with handlers that execute immediately on the current thread.
        /// 
        /// Use this for synchronous event processing where you need the handler to complete
        /// before the event sender continues. This blocks the sender until the handler finishes.
        /// 
        /// Good for critical event processing or when you need guaranteed immediate execution.
        /// </summary>
        /// <typeparam name="T">The type of event to handle</typeparam>
        /// <param name="handler">The function to call when this event is received</param>
        /// <param name="aggregateId">Game object ID for routed events, or Guid.Empty for global handlers</param>
        public static void RegisterImmediateHandler<T>(Action<T> handler, Guid aggregateId = default) where T : class
        {
            RegisterHandler(handler, aggregateId, new ImmediateDispatcher());
        }

        /// <summary>
        /// Subscribes to events with handlers that run on background threads.
        /// 
        /// Use this for CPU-intensive processing, file I/O, network operations, or any work
        /// that doesn't need to access Unity APIs. This keeps the main thread responsive.
        /// 
        /// Perfect for data processing, analytics, or any heavy computation triggered by events.
        /// </summary>
        /// <typeparam name="T">The type of event to handle</typeparam>
        /// <param name="handler">The function to call when this event is received</param>
        /// <param name="aggregateId">Game object ID for routed events, or Guid.Empty for global handlers</param>
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
        /// Registers a global handler for events of a specific type.
        /// 
        /// This method is deprecated. Use RegisterGlobalHandler or RegisterHandler instead.
        /// This method registers handlers that receive all events of the specified type
        /// regardless of routing (equivalent to RegisterHandler with Guid.Empty).
        /// </summary>
        /// <typeparam name="T">The type of event to handle</typeparam>
        /// <param name="handler">The function to call when this event is received</param>
        /// <param name="dispatcher">Which thread to run the handler on (defaults to background thread)</param>
        [Obsolete("Use RegisterGlobalHandler or RegisterHandler instead. This method is deprecated and will be removed in a future version.")]
        public static void RegisterStaticHandler<T>(Action<T> handler, IDispatcher? dispatcher = null) where T : class
        {
            EventBusCore.RegisterStaticHandler(handler, dispatcher);
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
    [Obsolete("The instance API is deprecated.")]
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
        /// Registers a global handler for events of a specific type.
        /// 
        /// This method is deprecated. Use EventBus.RegisterGlobalHandler or EventBus.RegisterHandler instead.
        /// This method registers handlers that receive all events of the specified type
        /// regardless of routing (equivalent to RegisterHandler with Guid.Empty).
        /// </summary>
        /// <typeparam name="T">The type of event to handle</typeparam>
        /// <param name="handler">The function to call when this event is received</param>
        /// <param name="dispatcher">Which thread to run the handler on (defaults to background thread)</param>
        [Obsolete("Use EventBus.RegisterGlobalHandler or EventBus.RegisterHandler instead. The instance API is deprecated and this method will be removed in a future version.")]
        public void RegisterStaticHandler<T>(Action<T> handler, IDispatcher? dispatcher = null) where T : class
        {
            EventBusCore.RegisterStaticHandler(handler, dispatcher);
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