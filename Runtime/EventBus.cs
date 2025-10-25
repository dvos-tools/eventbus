#nullable enable
using System;
using System.Collections.Generic;
using com.DvosTools.bus.Core;
using com.DvosTools.bus.Dispatchers;

namespace com.DvosTools.bus
{
    /// <summary>
    /// Central event communication hub for applications.
    /// <para>
    /// The EventBus enables loose coupling between systems by allowing any component to publish events
    /// that other components can subscribe to. Events can be sent immediately or buffered until specific
    /// aggregates are ready to process them.
    /// </para>
    /// <para>
    /// Key capabilities:
    /// <list type="bullet">
    /// <item>Publish events that any number of subscribers can receive</item>
    /// <item>Route events to specific aggregates using aggregate IDs</item>
    /// <item>Buffer events until aggregates are ready to handle them</item>
    /// <item>Execute event handlers on different threads (main thread, background, or immediate)</item>
    /// <item>Maintain event order and ensure thread safety</item>
    /// </list>
    /// </para>
    /// <para>
    /// This is the primary interface for all event communication in your application.
    /// </para>
    /// </summary>
    public static class EventBus
    {
        private static readonly EventBusCore CoreEventBus = EventBusCore.Instance;
        private static readonly EventBusService EventBusService = EventBusCore.Service;

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
        /// Queues the event for processing and returns immediately. For routed events,
        /// only handlers registered for the specific aggregate will receive the event.
        /// 
        /// Use this for fire-and-forget event publishing.
        /// </summary>
        /// <typeparam name="T">The type of event to send</typeparam>
        /// <param name="eventData">The event data to send</param>
        /// <example>
        /// <code lang="csharp">
        /// // Define a simple event
        /// public class PlayerDiedEvent
        /// {
        ///     public string PlayerName { get; set; }
        ///     public int Score { get; set; }
        /// }
        /// 
        /// // Send the event
        /// EventBus.Send(new PlayerDiedEvent 
        /// { 
        ///     PlayerName = "Player1", 
        ///     Score = 1000 
        /// });
        /// </code>
        /// </example>
        public static void Send<T>(T eventData) where T : class
        {
            EventBusService.Send(eventData);
        }

        /// <summary>
        /// Publishes an event and waits for all handlers to complete processing.
        /// 
        /// Blocks the current thread until all handlers finish executing. Use this for
        /// critical events where you need guaranteed processing order.
        /// </summary>
        /// <typeparam name="T">The type of event to send</typeparam>
        /// <param name="eventData">The event data to send</param>
        /// <example>
        /// <code>
        /// // Send a critical event and wait for all handlers to complete
        /// EventBus.SendAndWait(new GameStateChangedEvent 
        /// { 
        ///     NewState = GameState.GameOver 
        /// });
        /// 
        /// // Now safe to proceed knowing all handlers have finished
        /// Debug.Log("All game over handlers have completed");
        /// </code>
        /// </example>
        public static void SendAndWait<T>(T eventData) where T : class
        {
            EventBusService.SendAndWait(eventData);
        }

        /// <summary>
        /// Subscribes to receive events of a specific type.
        /// 
        /// Multiple handlers can be registered for the same event type. For routed events,
        /// specify an aggregateId to receive events for that specific object. Use Guid.Empty
        /// for global handlers. The dispatcher determines which thread your handler runs on.
        /// </summary>
        /// <typeparam name="T">The type of event to handle</typeparam>
        /// <param name="handler">The function to call when this event is received</param>
        /// <param name="aggregateId">Game object ID for routed events, or Guid.Empty for global handlers</param>
        /// <param name="dispatcher">Which thread to run the handler on (defaults to background thread)</param>
        /// <example>
        /// <code lang="csharp">
        /// // Global handler - receives all PlayerDiedEvent events
        /// EventBus.RegisterHandler&lt;PlayerDiedEvent&gt;(OnPlayerDied);
        /// 
        /// // Routed handler - only receives events for specific player
        /// EventBus.RegisterHandler&lt;PlayerHealthChangedEvent&gt;(OnHealthChanged, playerId);
        /// 
        /// // With custom dispatcher
        /// EventBus.RegisterHandler&lt;DataProcessedEvent&gt;(OnDataProcessed, Guid.Empty, new ThreadPoolDispatcher());
        /// 
        /// void OnPlayerDied(PlayerDiedEvent evt)
        /// {
        ///     Debug.Log("Player " + evt.PlayerName + " died with score " + evt.Score);
        /// }
        /// </code>
        /// </example>
        public static void RegisterHandler<T>(Action<T> handler, Guid aggregateId = default, IDispatcher? dispatcher = null) where T : class
        {
            EventBusService.RegisterHandler(handler, aggregateId, dispatcher);
        }

        /// <summary>
        /// Disposes all handlers for a specific event type and clears its queued events.
        /// </summary>
        /// <typeparam name="T">The type of event to dispose handlers for</typeparam>
        /// <example>
        /// <code>
        /// // Clean up when switching game modes
        /// EventBus.DisposeHandlers&lt;CombatEvent&gt;();
        /// EventBus.DisposeHandlers&lt;ExplorationEvent&gt;();
        /// </code>
        /// </example>
        public static void DisposeHandlers<T>() where T : class
        {
            CoreEventBus.DisposeHandlers<T>();
        }

        /// <summary>
        /// Disposes all handlers and clears all events.
        /// </summary>
        public static void DisposeAllHandlers()
        {
            CoreEventBus.DisposeAllHandlers();
        }

        /// <summary>
        /// Clears all queued and buffered events for a specific aggregate.
        /// </summary>
        /// <param name="aggregateId">The aggregate ID to clear events for</param>
        /// <example>
        /// <code>
        /// // Clear all events for a player when they disconnect
        /// EventBus.ClearEventsForAggregate(playerId);
        /// 
        /// // Clear events when an object becomes inactive
        /// void OnDisable()
        /// {
        ///     EventBus.ClearEventsForAggregate(gameObjectId);
        /// }
        /// </code>
        /// </example>
        public static void ClearEventsForAggregate(Guid aggregateId)
        {
            CoreEventBus.ClearEventsForAggregate(aggregateId);
        }

        /// <summary>
        /// Resets an aggregate by removing all its handlers and clearing all its events.
        /// </summary>
        /// <param name="aggregateId">The aggregate ID to reset</param>
        public static void ResetAggregate(Guid aggregateId)
        {
            CoreEventBus.ResetAggregate(aggregateId);
        }

        /// <summary>
        /// Disposes handlers for a specific event type and aggregate, and clears all its events.
        /// </summary>
        /// <typeparam name="T">The type of event to dispose handlers for</typeparam>
        /// <param name="aggregateId">The aggregate ID to dispose handlers for</param>
        public static void DisposeHandlersForAggregate<T>(Guid aggregateId) where T : class
        {
            CoreEventBus.DisposeHandlersForAggregate<T>(aggregateId);
        }

        /// <summary>
        /// Disposes a specific handler from an aggregate and clears all its events.
        /// </summary>
        /// <param name="handler">The handler function to dispose</param>
        /// <param name="aggregateId">The aggregate ID to dispose the handler from</param>
        public static void DisposeHandlerFromAggregate<T>(Action<T> handler, Guid aggregateId) where T : class
        {
            EventBusService.DisposeHandlerFromAggregate(handler, aggregateId);
        }

        /// <summary>
        /// Gets the number of registered handlers for an aggregate.
        /// </summary>
        /// <param name="aggregateId">The aggregate ID to check</param>
        /// <returns>Number of registered handlers for the aggregate</returns>
        public static int GetHandlerCountForAggregate(Guid aggregateId)
        {
            return CoreEventBus.GetHandlerCountForAggregate(aggregateId);
        }

        /// <summary>
        /// Checks if there are any handlers registered for an aggregate.
        /// </summary>
        /// <param name="aggregateId">The aggregate ID to check</param>
        /// <returns>True if handlers are registered for the aggregate, false otherwise</returns>
        public static bool HasHandlersForAggregate(Guid aggregateId)
        {
            return CoreEventBus.HasHandlersForAggregate(aggregateId);
        }

        /// <summary>
        /// Clears all handlers, buffered events, and queued events.
        /// </summary>
        public static void ClearAll()
        {
            CoreEventBus.ClearAll();
        }

        /// <summary>
        /// Signals that a game object is ready to process its buffered events.
        /// 
        /// When routed events are sent to objects that aren't ready yet, they get buffered.
        /// Call this when the object is initialized and ready to handle events. All buffered
        /// events for this aggregate will be processed immediately.
        /// </summary>
        /// <param name="aggregateId">The game object ID that is now ready to process events</param>
        /// <example>
        /// <code lang="csharp">
        /// public class PlayerController : MonoBehaviour
        /// {
        ///     private Guid playerId;
        ///     
        ///     void Start()
        ///     {
        ///         playerId = Guid.NewGuid();
        ///         
        ///         // Register handler for this player
        ///         EventBus.RegisterUnityHandler&lt;PlayerHealthChangedEvent&gt;(OnHealthChanged, playerId);
        ///         
        ///         // Signal that this player is ready to process buffered events
        ///         EventBus.AggregateReady(playerId);
        ///     }
        ///     
        ///     void OnHealthChanged(PlayerHealthChangedEvent evt)
        ///     {
        ///         // This will receive any events that were buffered before Start() was called
        ///         UpdateHealthUI(evt.CurrentHealth);
        ///     }
        /// }
        /// </code>
        /// </example>
        public static void AggregateReady(Guid aggregateId)
        {
            EventBusService.AggregateReady(aggregateId);
        }

        /// <summary>
        /// Gets the number of buffered events for an aggregate.
        /// </summary>
        /// <param name="aggregateId">The aggregate ID to check</param>
        /// <returns>The number of buffered events</returns>
        /// <example>
        /// <code>
        /// // Check how many events are waiting for a player
        /// int bufferedCount = EventBus.GetBufferedEventCount(playerId);
        /// if (bufferedCount > 100)
        /// {
        ///     Debug.LogWarning("Player " + playerId + " has " + bufferedCount + " buffered events");
        /// }
        /// </code>
        /// </example>
        public static int GetBufferedEventCount(Guid aggregateId)
        {
            return CoreEventBus.GetBufferedEventCount(aggregateId);
        }

        /// <summary>
        /// Gets all aggregate IDs with buffered events.
        /// </summary>
        /// <returns>Collection of aggregate IDs with buffered events</returns>
        public static IEnumerable<Guid> GetBufferedAggregateIds()
        {
            return CoreEventBus.GetBufferedAggregateIds();
        }

        /// <summary>
        /// Gets the total number of buffered events.
        /// </summary>
        /// <returns>Total number of buffered events</returns>
        public static int GetTotalBufferedEventCount()
        {
            return CoreEventBus.GetTotalBufferedEventCount();
        }

        /// <summary>
        /// Gets the number of events in the processing queue.
        /// </summary>
        /// <returns>Number of events in the queue</returns>
        /// <example>
        /// <code>
        /// // Monitor system health
        /// int queueSize = EventBus.GetQueueCount();
        /// if (queueSize > 1000)
        /// {
        ///     Debug.LogWarning("High event queue size: " + queueSize);
        /// }
        /// </code>
        /// </example>
        public static int GetQueueCount()
        {
            lock (CoreEventBus.QueueLock)
            {
                return CoreEventBus.EventQueue.Count;
            }
        }

        /// <summary>
        /// Gets the number of registered handlers for an event type.
        /// </summary>
        /// <typeparam name="T">The type of event to check</typeparam>
        /// <returns>Number of registered handlers</returns>
        /// <example>
        /// <code>
        /// // Check how many handlers are listening for player events
        /// int handlerCount = EventBus.GetHandlerCount&lt;PlayerDiedEvent&gt;();
        /// Debug.Log("There are " + handlerCount + " handlers for PlayerDiedEvent");
        /// </code>
        /// </example>
        public static int GetHandlerCount<T>() where T : class
        {
            var eventType = typeof(T);
            lock (CoreEventBus.HandlersLock)
            {
                return CoreEventBus.Handlers.TryGetValue(eventType, out var handlers) ? handlers.Count : 0;
            }
        }

        /// <summary>
        /// Checks if there are any handlers registered for an event type.
        /// </summary>
        /// <typeparam name="T">The type of event to check</typeparam>
        /// <returns>True if handlers are registered, false otherwise</returns>
        /// <example>
        /// <code>
        /// // Check if anyone is listening before sending an event
        /// if (EventBus.HasHandlers&lt;PlayerDiedEvent&gt;())
        /// {
        ///     EventBus.Send(new PlayerDiedEvent { PlayerName = "Player1" });
        /// }
        /// else
        /// {
        ///     Debug.Log("No handlers for PlayerDiedEvent, skipping send");
        /// }
        /// </code>
        /// </example>
        public static bool HasHandlers<T>() where T : class
        {
            return GetHandlerCount<T>() > 0;
        }

        /// <summary>
        /// Shuts down the event bus.
        /// </summary>
        public static void Shutdown()
        {
            CoreEventBus.Shutdown();
        }

        /// <summary>
        /// Cleans up all resources and resets the event bus state.
        /// </summary>
        public static void Cleanup()
        {
            // Cleanup Unity-specific dispatchers first
            try
            {
                BusHelper.Instance?.Cleanup();
                UnityDispatcher.Instance?.Cleanup();
            }
            catch
            {
                // ignored
            }

            // Cleanup the core event bus (this clears all handlers and their dispatchers)
            CoreEventBus.Dispose();
        }

        /// <summary>
        /// Subscribes to events with handlers that run on Unity's main thread.
        /// 
        /// Use this when your handler needs to access Unity APIs or update the UI.
        /// All Unity API calls must happen on the main thread.
        /// </summary>
        /// <typeparam name="T">The type of event to handle</typeparam>
        /// <param name="handler">The function to call when this event is received</param>
        /// <param name="aggregateId">Aggregate ID for routed events, or Guid.Empty for global handlers</param>
        /// <example>
        /// <code>
        /// // Register a handler that updates UI (must run on main thread)
        /// EventBus.RegisterUnityHandler&lt;PlayerHealthChangedEvent&gt;(OnHealthChanged);
        /// 
        /// // Register a routed handler for specific player
        /// EventBus.RegisterUnityHandler&lt;PlayerMovedEvent&gt;(OnPlayerMoved, playerId);
        /// 
        /// void OnHealthChanged(PlayerHealthChangedEvent evt)
        /// {
        ///     // Safe to use Unity APIs here
        ///     healthBar.fillAmount = (float)evt.CurrentHealth / evt.MaxHealth;
        ///     healthText.text = evt.CurrentHealth + "/" + evt.MaxHealth;
        /// }
        /// 
        /// void OnPlayerMoved(PlayerMovedEvent evt)
        /// {
        ///     // Safe to access Unity objects
        ///     transform.position = evt.Position;
        /// }
        /// </code>
        /// </example>
        public static void RegisterUnityHandler<T>(Action<T> handler, Guid aggregateId = default) where T : class
        {
            try
            {
                RegisterHandler(handler, aggregateId, UnityDispatcher.Instance);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("UnityDispatcher is not available. Make sure you're running in a Unity environment.", ex);
            }
        }

        /// <summary>
        /// Subscribes to events with handlers that execute immediately on the current thread.
        /// 
        /// Use this for synchronous event processing. This blocks the sender until the
        /// handler finishes. Good for critical event processing.
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
        /// Use this for CPU-intensive processing, file I/O, or any work that doesn't need
        /// to access Unity APIs. This keeps the main thread responsive.
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
        /// </summary>
        /// <typeparam name="T">The type of event to handle</typeparam>
        /// <param name="handler">The handler function</param>
        /// <param name="aggregateId">The aggregate ID for routing</param>
        /// <param name="dispatcher">Optional dispatcher for handling the event</param>
        /// <example>
        /// <code>
        /// // Register a routed handler for a specific player
        /// EventBus.RegisterRoutedHandler&lt;PlayerHealthChangedEvent&gt;(OnHealthChanged, playerId);
        /// 
        /// // Register with custom dispatcher
        /// EventBus.RegisterRoutedHandler&lt;DataProcessedEvent&gt;(OnDataProcessed, aggregateId, new ThreadPoolDispatcher());
        /// 
        /// void OnHealthChanged(PlayerHealthChangedEvent evt)
        /// {
        ///     // This handler only receives events for the specific player
        ///     UpdatePlayerHealth(evt.CurrentHealth);
        /// }
        /// </code>
        /// </example>
        public static void RegisterRoutedHandler<T>(Action<T> handler, Guid aggregateId, IDispatcher? dispatcher = null) where T : class
        {
            if (aggregateId == Guid.Empty)
                throw new ArgumentException("Aggregate ID cannot be empty for routed handlers", nameof(aggregateId));
                
            RegisterHandler(handler, aggregateId, dispatcher);
        }

        /// <summary>
        /// Registers a global handler that will receive all events of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of event to handle</typeparam>
        /// <param name="handler">The handler function</param>
        /// <param name="dispatcher">Optional dispatcher for handling the event</param>
        /// <example>
        /// <code>
        /// // Register a global handler for all game state changes
        /// EventBus.RegisterGlobalHandler&lt;GameStateChangedEvent&gt;(OnGameStateChanged);
        /// 
        /// // Register with custom dispatcher for background processing
        /// EventBus.RegisterGlobalHandler&lt;AnalyticsEvent&gt;(OnAnalyticsEvent, new ThreadPoolDispatcher());
        /// 
        /// void OnGameStateChanged(GameStateChangedEvent evt)
        /// {
        ///     // This handler receives ALL GameStateChangedEvent events
        ///     Debug.Log("Game state changed to: " + evt.NewState);
        /// }
        /// </code>
        /// </example>
        public static void RegisterGlobalHandler<T>(Action<T> handler, IDispatcher? dispatcher = null) where T : class
        {
            RegisterHandler(handler, Guid.Empty, dispatcher);
        }

        /// <summary>
        /// [DEPRECATED] Registers a global handler for events.
        /// </summary>
        /// <typeparam name="T">The type of event to handle</typeparam>
        /// <param name="handler">The function to call when this event is received</param>
        /// <param name="dispatcher">Which thread to run the handler on (defaults to background thread)</param>
        [Obsolete("Use RegisterGlobalHandler or RegisterHandler instead. This method is deprecated and will be removed in a future version.")]
        public static void RegisterStaticHandler<T>(Action<T> handler, IDispatcher? dispatcher = null) where T : class
        {
            EventBusService.RegisterHandler(handler, Guid.Empty, dispatcher);
        }

        /// <summary>
        /// Checks if there are any buffered events.
        /// </summary>
        /// <returns>True if there are any buffered events, false otherwise</returns>
        /// <example>
        /// <code>
        /// // Check if any events are waiting to be processed
        /// if (EventBus.HasBufferedEvents())
        /// {
        ///     Debug.Log("There are buffered events waiting to be processed");
        ///     var totalBuffered = EventBus.GetTotalBufferedEventCount();
        ///     Debug.Log("Total buffered events: " + totalBuffered);
        /// }
        /// </code>
        /// </example>
        public static bool HasBufferedEvents()
        {
            return GetTotalBufferedEventCount() > 0;
        }

        /// <summary>
        /// Checks if there are any events in the processing queue.
        /// </summary>
        /// <returns>True if there are events in the queue, false otherwise</returns>
        /// <example>
        /// <code>
        /// // Check if events are currently being processed
        /// if (EventBus.HasQueuedEvents())
        /// {
        ///     int queueSize = EventBus.GetQueueCount();
        ///     Debug.Log("There are " + queueSize + " events in the processing queue");
        /// }
        /// </code>
        /// </example>
        public static bool HasQueuedEvents()
        {
            return GetQueueCount() > 0;
        }
    }

    /// <summary>
    /// [DEPRECATED] Wrapper class for backwards compatibility with the instance API.
    /// Use the static EventBus methods directly instead.
    /// </summary>
    [Obsolete("The instance API is deprecated.")]
    public class EventBusInstance
    {
        /// <summary>
        /// [DEPRECATED] Sends an event asynchronously.
        /// </summary>
        [Obsolete("Use EventBus.Send() instead of EventBus.Instance.Send().")]
        public void Send<T>(T eventData) where T : class => EventBus.Send(eventData);

        /// <summary>
        /// [DEPRECATED] Sends an event synchronously and waits for completion.
        /// </summary>
        [Obsolete("Use EventBus.SendAndWait() instead of EventBus.Instance.SendAndWait().")]
        public void SendAndWait<T>(T eventData) where T : class => EventBus.SendAndWait(eventData);

        /// <summary>
        /// [DEPRECATED] Marks an aggregate as ready.
        /// </summary>
        [Obsolete("Use EventBus.AggregateReady() instead of EventBus.Instance.AggregateReady().")]
        public void AggregateReady(Guid aggregateId) => EventBus.AggregateReady(aggregateId);

        /// <summary>
        /// [DEPRECATED] Gets the number of buffered events for an aggregate.
        /// </summary>
        [Obsolete("Use EventBus.GetBufferedEventCount() instead of EventBus.Instance.GetBufferedEventCount().")]
        public int GetBufferedEventCount(Guid aggregateId) => EventBus.GetBufferedEventCount(aggregateId);

        /// <summary>
        /// [DEPRECATED] Gets all aggregate IDs with buffered events.
        /// </summary>
        [Obsolete("Use EventBus.GetBufferedAggregateIds() instead of EventBus.Instance.GetBufferedAggregateIds().")]
        public IEnumerable<Guid> GetBufferedAggregateIds() => EventBus.GetBufferedAggregateIds();

        /// <summary>
        /// [DEPRECATED] Gets the total number of buffered events.
        /// </summary>
        [Obsolete("Use EventBus.GetTotalBufferedEventCount() instead of EventBus.Instance.GetTotalBufferedEventCount().")]
        public int GetTotalBufferedEventCount() => EventBus.GetTotalBufferedEventCount();

        /// <summary>
        /// [DEPRECATED] Gets the number of events in the processing queue.
        /// </summary>
        [Obsolete("Use EventBus.GetQueueCount() instead of EventBus.Instance.GetQueueCount().")]
        public int GetQueueCount() => EventBus.GetQueueCount();

        /// <summary>
        /// [DEPRECATED] Gets the number of registered handlers for an event type.
        /// </summary>
        [Obsolete("Use EventBus.GetHandlerCount() instead of EventBus.Instance.GetHandlerCount().")]
        public int GetHandlerCount<T>() where T : class => EventBus.GetHandlerCount<T>();

        /// <summary>
        /// [DEPRECATED] Checks if there are handlers for an event type.
        /// </summary>
        [Obsolete("Use EventBus.HasHandlers() instead of EventBus.Instance.HasHandlers().")]
        public bool HasHandlers<T>() where T : class => EventBus.HasHandlers<T>();

        /// <summary>
        /// [DEPRECATED] Registers a global handler for events.
        /// </summary>
        [Obsolete("Use EventBus.RegisterGlobalHandler or EventBus.RegisterHandler instead.")]
        public void RegisterStaticHandler<T>(Action<T> handler, IDispatcher? dispatcher = null) where T : class
        {
            EventBusService.RegisterHandler(handler, Guid.Empty, dispatcher);
        }

        /// <summary>
        /// [DEPRECATED] Shuts down the event bus.
        /// </summary>
        [Obsolete("Use EventBus.Shutdown() instead of EventBus.Instance.Shutdown().")]
        public void Shutdown() => EventBus.Shutdown();

        /// <summary>
        /// [DEPRECATED] Direct access to handlers.
        /// </summary>
        [Obsolete("Use EventBus.GetHandlerCount() and EventBus.HasHandlers() instead.")]
        public Dictionary<Type, List<Subscription>> Handlers => EventBusCore.Instance.Handlers;

        /// <summary>
        /// [DEPRECATED] Direct access to event queue.
        /// </summary>
        [Obsolete("Use EventBus.GetQueueCount() and EventBus.HasQueuedEvents() instead.")]
        public Queue<QueuedEvent> EventQueue => EventBusCore.Instance.EventQueue;

        /// <summary>
        /// [DEPRECATED] Direct access to queue lock.
        /// </summary>
        [Obsolete("Use the static API methods instead.")]
        public object QueueLock => EventBusCore.Instance.QueueLock;
    }
}