#nullable enable
using System;
using System.Collections.Generic;
using com.DvosTools.bus.Core;
using com.DvosTools.bus.Dispatchers;

namespace com.DvosTools.bus
{
    /// <summary>
    /// Central event communication hub for applications.
    ///
    /// Enables loose coupling between systems by allowing components to publish and subscribe to events.
    /// Events can be sent immediately or buffered until aggregates are ready to process them.
    /// Event payloads may be reference or value types (<c>class</c> or <c>struct</c>); publish APIs take
    /// <c>in T</c> so large structs are not copied on the hot path.
    ///
    /// Key capabilities:
    /// <list type="bullet">
    /// <item>Publish events to any number of subscribers</item>
    /// <item>Route events to specific aggregates using aggregate IDs</item>
    /// <item>Buffer events until aggregates are ready to handle them</item>
    /// <item>Execute handlers on different threads (Unity main via <see cref="RegisterUnityHandler{T}"/>, background pool via <see cref="RegisterBackgroundHandler{T}"/>, or a custom <see cref="IDispatcher"/>)</item>
    /// <item>Maintain event order and ensure thread safety</item>
    /// </list>
    ///
    /// Hot path: <see cref="SendAndWait{T}"/> + a main-thread subscriber registered with <see cref="RegisterUnityHandler{T}"/> avoids per-call allocations on the subscriber path when tuned for that scenario.
    /// </summary>
    public static class EventBus
    {
        /// <summary>
        /// [DEPRECATED] Gets the EventBus instance for backwards compatibility.
        /// Use static methods directly instead (e.g., <c>EventBus.Send(...)</c> not <c>EventBus.Instance.Send(...)</c>).
        /// </summary>
        [Obsolete("The instance API is deprecated. Use static methods directly (EventBus.Send, etc.).")]
        public static EventBusInstance Instance => new EventBusInstance();

        // ===== EVENT PUBLISHING =====

        /// <summary>
        /// Publishes an event to all registered subscribers.
        ///
        /// Queues the event for processing and returns immediately. For routed events,
        /// only handlers registered for the specific aggregate will receive the event.
        ///
        /// Use this for fire-and-forget event publishing.
        /// </summary>
        ///
        /// <typeparam name="T">The type of event to send. Prefer <c>struct</c> events — they live on the stack,
        /// produce no heap allocation, and are passed by reference via <c>in</c>, so even large payloads
        /// have zero copy overhead on the publish side.</typeparam>
        /// <param name="eventData">The event data to send.</param>
        /// <param name="aggregateId">When not <see cref="Guid.Empty"/>, routes the event to handlers registered for this aggregate; otherwise dispatches globally.</param>
        ///
        /// <example>
        /// <code lang="csharp">
        /// // Preferred: struct event — no heap allocation, passed by ref via 'in'
        /// public readonly struct PlayerDiedEvent
        /// {
        ///     public readonly string PlayerName;
        ///     public readonly int Score;
        ///     public PlayerDiedEvent(string name, int score) { PlayerName = name; Score = score; }
        /// }
        ///
        /// EventBus.Send(new PlayerDiedEvent("Player1", 1000));
        ///
        /// // Routed: only handlers registered for playerId receive it
        /// EventBus.Send(new PlayerDiedEvent("P2", 500), playerId);
        ///
        /// // Also works with class events when reference semantics are needed
        /// public class GameSessionEvent { public string MapName { get; set; } }
        /// EventBus.Send(new GameSessionEvent { MapName = "Level1" });
        /// </code>
        /// </example>
        public static void Send<T>(in T eventData, Guid aggregateId = default)
            => EventBusService.Send(in eventData, aggregateId);

        /// <summary>
        /// Publishes an event and waits for all handlers to complete processing.
        ///
        /// Blocks the current thread until all handlers finish executing. Use this for
        /// critical events where you need guaranteed processing order.
        /// </summary>
        ///
        /// <typeparam name="T">The type of event to send. Prefer <c>struct</c> events — they produce no
        /// heap allocation and are passed by reference via <c>in</c>, making <see cref="SendAndWait{T}"/>
        /// with struct events the zero-alloc hot path.</typeparam>
        /// <param name="eventData">The event data to send.</param>
        /// <param name="aggregateId">When not <see cref="Guid.Empty"/>, routes the event to handlers registered for this aggregate; otherwise dispatches globally.</param>
        ///
        /// <example>
        /// <code lang="csharp">
        /// // Preferred: struct event on the hot path — zero heap allocation end-to-end
        /// public readonly struct GameStateChangedEvent
        /// {
        ///     public readonly GameState NewState;
        ///     public GameStateChangedEvent(GameState state) { NewState = state; }
        /// }
        ///
        /// EventBus.SendAndWait(new GameStateChangedEvent(GameState.GameOver));
        /// UnityEngine.Debug.Log("All game-over handlers have completed");
        ///
        /// // Also works with class events when reference semantics are needed
        /// public class LevelLoadedEvent { public string SceneName { get; set; } }
        /// EventBus.SendAndWait(new LevelLoadedEvent { SceneName = "Boss" });
        /// UnityEngine.Debug.Log("All level-loaded handlers have completed");
        /// </code>
        /// </example>
        public static void SendAndWait<T>(in T eventData, Guid aggregateId = default)
            => EventBusService.SendAndWait(in eventData, aggregateId);

        // ===== EVENT SUBSCRIPTION =====

        /// <summary>
        /// Subscribes to receive events of a specific type.
        ///
        /// Multiple handlers can be registered for the same event type. For routed events,
        /// specify an aggregateId to receive events for that specific object. Use <see cref="Guid.Empty"/>
        /// for global handlers. The dispatcher determines which thread your handler runs on.
        /// </summary>
        ///
        /// <typeparam name="T">The type of event to handle.</typeparam>
        /// <param name="handler">The function to call when this event is received.</param>
        /// <param name="aggregateId">Aggregate ID for routed events, or <see cref="Guid.Empty"/> for global handlers.</param>
        /// <param name="dispatcher">Which execution context runs the handler. Defaults to <c>null</c>, which lets the event bus select a default dispatcher. Pass <see cref="UnityDispatcher"/> for the Unity main thread or <see cref="ThreadPoolDispatcher"/> for a background thread.</param>
        ///
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
        ///     UnityEngine.Debug.Log("Player " + evt.PlayerName + " died with score " + evt.Score);
        /// }
        /// </code>
        /// </example>
        public static void RegisterHandler<T>(Action<T> handler, Guid aggregateId = default, IDispatcher? dispatcher = null)
            => EventBusService.RegisterHandler(handler, aggregateId, dispatcher);

        /// <summary>
        /// Subscribes to events with handlers that run on Unity's main thread.
        ///
        /// Use this when your handler needs to access Unity APIs or update the UI.
        /// All Unity API calls must happen on the main thread.
        /// </summary>
        ///
        /// <typeparam name="T">The type of event to handle.</typeparam>
        /// <param name="handler">The function to call when this event is received.</param>
        /// <param name="aggregateId">Aggregate ID for routed events, or <see cref="Guid.Empty"/> for global handlers.</param>
        ///
        /// <exception cref="InvalidOperationException">Thrown when <see cref="UnityDispatcher"/> is not available (non-Unity host).</exception>
        ///
        /// <example>
        /// <code lang="csharp">
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
        public static void RegisterUnityHandler<T>(Action<T> handler, Guid aggregateId = default)
        {
            try { RegisterHandler(handler, aggregateId, UnityDispatcher.Instance); }
            catch (Exception ex)
            {
                throw new InvalidOperationException("UnityDispatcher is not available. Make sure you're running in a Unity environment.", ex);
            }
        }

        /// <summary>
        /// Subscribes to events with handlers that run on background threads.
        ///
        /// Use this for CPU-intensive processing, file I/O, or any work that doesn't need
        /// to access Unity APIs. This keeps the main thread responsive.
        /// </summary>
        ///
        /// <typeparam name="T">The type of event to handle.</typeparam>
        /// <param name="handler">The function to call when this event is received.</param>
        /// <param name="aggregateId">Aggregate ID for routed events, or <see cref="Guid.Empty"/> for global handlers.</param>
        public static void RegisterBackgroundHandler<T>(Action<T> handler, Guid aggregateId = default)
            => RegisterHandler(handler, aggregateId, new ThreadPoolDispatcher());

        /// <summary>
        /// Registers a handler for routed events and immediately flushes any buffered events of
        /// type <typeparamref name="T"/> that were already queued for this aggregate.
        ///
        /// Combines <see cref="RegisterHandler{T}"/> and a partial <see cref="AggregateReady(Guid, Delegate[])"/>
        /// into a single call, so events sent before the handler was registered are not lost.
        /// </summary>
        ///
        /// <typeparam name="T">The type of event to handle.</typeparam>
        /// <param name="handler">The handler function.</param>
        /// <param name="aggregateId">The aggregate ID for routing; must not be <see cref="Guid.Empty"/>.</param>
        /// <param name="dispatcher">Optional dispatcher for handling the event.</param>
        ///
        /// <example>
        /// <code lang="csharp">
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
        public static void RegisterRoutedHandler<T>(Action<T> handler, Guid aggregateId, IDispatcher? dispatcher = null)
        {
            if (aggregateId == Guid.Empty)
                throw new ArgumentException("Aggregate ID cannot be empty for routed handlers", nameof(aggregateId));
            RegisterHandler(handler, aggregateId, dispatcher);
            AggregateReady(aggregateId, handler);
        }

        /// <summary>
        /// Registers a global handler that will receive all events of the specified type.
        /// </summary>
        ///
        /// <typeparam name="T">The type of event to handle.</typeparam>
        /// <param name="handler">The handler function.</param>
        /// <param name="dispatcher">Optional dispatcher for handling the event.</param>
        ///
        /// <example>
        /// <code lang="csharp">
        /// // Register a global handler for all game state changes
        /// EventBus.RegisterGlobalHandler&lt;GameStateChangedEvent&gt;(OnGameStateChanged);
        ///
        /// // Register with custom dispatcher for background processing
        /// EventBus.RegisterGlobalHandler&lt;AnalyticsEvent&gt;(OnAnalyticsEvent, new ThreadPoolDispatcher());
        ///
        /// void OnGameStateChanged(GameStateChangedEvent evt)
        /// {
        ///     // This handler receives ALL GameStateChangedEvent events
        ///     UnityEngine.Debug.Log("Game state changed to: " + evt.NewState);
        /// }
        /// </code>
        /// </example>
        public static void RegisterGlobalHandler<T>(Action<T> handler, IDispatcher? dispatcher = null)
            => EventBusService.RegisterHandler(handler, Guid.Empty, dispatcher);

        /// <summary>
        /// [DEPRECATED] Registers a global handler for events.
        /// </summary>
        ///
        /// <typeparam name="T">The type of event to handle.</typeparam>
        /// <param name="handler">The function to call when this event is received.</param>
        /// <param name="dispatcher">Optional dispatcher for handling the event.</param>
        [Obsolete("Use RegisterGlobalHandler or RegisterHandler instead.")]
        public static void RegisterStaticHandler<T>(Action<T> handler, IDispatcher? dispatcher = null)
            => EventBusService.RegisterHandler(handler, Guid.Empty, dispatcher);

        // ===== LIFECYCLE =====

        /// <summary>
        /// Disposes all handlers for a specific event type and clears its queued events.
        /// </summary>
        ///
        /// <typeparam name="T">The type of event to dispose handlers for.</typeparam>
        ///
        /// <example>
        /// <code lang="csharp">
        /// // Clean up when switching game modes
        /// EventBus.DisposeHandlers&lt;CombatEvent&gt;();
        /// EventBus.DisposeHandlers&lt;ExplorationEvent&gt;();
        /// </code>
        /// </example>
        public static void DisposeHandlers<T>() => EventBusService.DisposeHandlers<T>();

        /// <summary>
        /// Disposes all handlers and clears all events.
        /// </summary>
        public static void DisposeAllHandlers() => EventBusCore.ClearAll();

        /// <summary>
        /// Clears all queued and buffered events for a specific aggregate.
        /// </summary>
        ///
        /// <param name="aggregateId">The aggregate ID to clear events for.</param>
        ///
        /// <example>
        /// <code lang="csharp">
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
        public static void ClearEventsForAggregate(Guid aggregateId) => EventBusCore.ClearEventsForAggregate(aggregateId);

        /// <summary>
        /// Resets an aggregate by removing all its handlers and clearing all its events.
        /// </summary>
        ///
        /// <param name="aggregateId">The aggregate ID to reset.</param>
        public static void ResetAggregate(Guid aggregateId) => EventBusCore.ResetAggregate(aggregateId);

        /// <summary>
        /// Disposes handlers for a specific event type and aggregate, and clears all its events.
        /// </summary>
        ///
        /// <typeparam name="T">The type of event to dispose handlers for.</typeparam>
        /// <param name="aggregateId">The aggregate ID to dispose handlers for.</param>
        public static void DisposeHandlersForAggregate<T>(Guid aggregateId) => EventBusService.DisposeHandlersForAggregate<T>(aggregateId);

        /// <summary>
        /// Disposes a specific handler from an aggregate and clears all its events.
        /// </summary>
        ///
        /// <param name="handler">The handler function to dispose.</param>
        /// <param name="aggregateId">The aggregate ID to dispose the handler from.</param>
        public static void DisposeHandlerFromAggregate<T>(Action<T> handler, Guid aggregateId) => EventBusService.DisposeHandlerFromAggregate(handler, aggregateId);

        /// <summary>
        /// Gets the number of registered handlers for an aggregate.
        /// </summary>
        ///
        /// <param name="aggregateId">The aggregate ID to check.</param>
        /// <returns>Number of registered handlers for the aggregate.</returns>
        public static int GetHandlerCountForAggregate(Guid aggregateId) => EventBusCore.GetHandlerCountForAggregate(aggregateId);

        /// <summary>
        /// Checks if there are any handlers registered for an aggregate.
        /// </summary>
        ///
        /// <param name="aggregateId">The aggregate ID to check.</param>
        /// <returns>True if handlers are registered for the aggregate, false otherwise.</returns>
        public static bool HasHandlersForAggregate(Guid aggregateId) => EventBusCore.HasHandlersForAggregate(aggregateId);

        /// <summary>
        /// Clears all handlers, buffered events, and queued events.
        /// </summary>
        public static void ClearAll() => EventBusCore.ClearAll();

        /// <summary>
        /// Signals that a game object is ready to process its buffered events.
        ///
        /// When routed events are sent to objects that aren't ready yet, they get buffered.
        /// Call this when the object is initialized and ready to handle events. All buffered
        /// events for this aggregate will be processed immediately.
        /// </summary>
        ///
        /// <param name="aggregateId">The game object ID that is now ready to process events.</param>
        ///
        /// <example>
        /// <code lang="csharp">
        /// public class PlayerController : UnityEngine.MonoBehaviour
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
        public static void AggregateReady(Guid aggregateId) => EventBusService.AggregateReady(aggregateId);

        /// <summary>
        /// Partial flush: scans the aggregate buffer and releases only events that correspond to one of
        /// the given delegate references (same instances passed to
        /// <see cref="RegisterHandler{T}"/> / <see cref="RegisterRoutedHandler{T}"/>).
        ///
        /// Each released event is delivered the same way as <see cref="AggregateReady(Guid)"/> (same
        /// dispatchers, global handlers, and all routed handlers for that aggregate and event type).
        /// Buffered events that do not match any listed handler stay in the buffer until a full
        /// <see cref="AggregateReady(Guid)"/> or another partial flush applies.
        /// </summary>
        ///
        /// <param name="aggregateId">The aggregate whose buffer is scanned.</param>
        /// <param name="handlers">Delegates that identify which buffered event kinds to release.</param>
        public static void AggregateReady(Guid aggregateId, params Delegate[] handlers) => EventBusService.AggregateReady(aggregateId, handlers);

        /// <summary>
        /// Gets the number of buffered events for an aggregate.
        /// </summary>
        ///
        /// <param name="aggregateId">The aggregate ID to check.</param>
        /// <returns>The number of buffered events.</returns>
        ///
        /// <example>
        /// <code lang="csharp">
        /// // Check how many events are waiting for a player
        /// int bufferedCount = EventBus.GetBufferedEventCount(playerId);
        /// if (bufferedCount > 100)
        /// {
        ///     UnityEngine.Debug.LogWarning("Player " + playerId + " has " + bufferedCount + " buffered events");
        /// }
        /// </code>
        /// </example>
        public static int GetBufferedEventCount(Guid aggregateId) => EventBusCore.GetBufferedEventCount(aggregateId);

        /// <summary>
        /// Gets all aggregate IDs with buffered events.
        /// </summary>
        ///
        /// <returns>Collection of aggregate IDs with buffered events.</returns>
        public static IEnumerable<Guid> GetBufferedAggregateIds() => EventBusCore.GetBufferedAggregateIds();

        /// <summary>
        /// Gets the total number of buffered events across all aggregates.
        /// </summary>
        ///
        /// <returns>Total number of buffered events.</returns>
        public static int GetTotalBufferedEventCount() => EventBusCore.GetTotalBufferedEventCount();

        /// <summary>
        /// Gets the number of events currently in the processing queue.
        /// </summary>
        ///
        /// <returns>Number of events in the queue.</returns>
        ///
        /// <example>
        /// <code lang="csharp">
        /// // Monitor system health
        /// int queueSize = EventBus.GetQueueCount();
        /// if (queueSize > 1000)
        /// {
        ///     UnityEngine.Debug.LogWarning("High event queue size: " + queueSize);
        /// }
        /// </code>
        /// </example>
        public static int GetQueueCount() => EventBusCore.GetTotalQueueCount();

        /// <summary>
        /// Gets the number of registered handlers for an event type.
        /// </summary>
        ///
        /// <typeparam name="T">The type of event to check.</typeparam>
        /// <returns>Number of registered handlers.</returns>
        ///
        /// <example>
        /// <code lang="csharp">
        /// // Check how many handlers are listening for player events
        /// int handlerCount = EventBus.GetHandlerCount&lt;PlayerDiedEvent&gt;();
        /// UnityEngine.Debug.Log("There are " + handlerCount + " handlers for PlayerDiedEvent");
        /// </code>
        /// </example>
        public static int GetHandlerCount<T>() => EventBusCore.GetHandlerCount<T>();

        /// <summary>
        /// Checks if there are any handlers registered for an event type.
        /// </summary>
        ///
        /// <typeparam name="T">The type of event to check.</typeparam>
        /// <returns>True if handlers are registered, false otherwise.</returns>
        ///
        /// <example>
        /// <code lang="csharp">
        /// // Check if anyone is listening before sending an event
        /// if (EventBus.HasHandlers&lt;PlayerDiedEvent&gt;())
        /// {
        ///     EventBus.Send(new PlayerDiedEvent("Player1", 1000));
        /// }
        /// else
        /// {
        ///     UnityEngine.Debug.Log("No handlers for PlayerDiedEvent, skipping send");
        /// }
        /// </code>
        /// </example>
        public static bool HasHandlers<T>() => GetHandlerCount<T>() > 0;

        /// <summary>
        /// Shuts down the event bus.
        /// </summary>
        public static void Shutdown() => EventBusCore.Shutdown();

        /// <summary>
        /// Cleans up all resources and resets the event bus state.
        /// </summary>
        public static void Cleanup()
        {
            try
            {
                BusHelper.Instance?.Cleanup();
                UnityDispatcher.Instance?.Cleanup();
            }
            catch { /* ignored */ }
            EventBusCore.Dispose();
        }

        // ===== UTILITY METHODS =====

        /// <summary>
        /// Checks if there are any buffered events across all aggregates.
        /// </summary>
        ///
        /// <returns>True if there are any buffered events, false otherwise.</returns>
        ///
        /// <example>
        /// <code lang="csharp">
        /// if (EventBus.HasBufferedEvents())
        /// {
        ///     var totalBuffered = EventBus.GetTotalBufferedEventCount();
        ///     UnityEngine.Debug.Log("Buffered events waiting to be processed: " + totalBuffered);
        /// }
        /// </code>
        /// </example>
        public static bool HasBufferedEvents() => GetTotalBufferedEventCount() > 0;

        /// <summary>
        /// Checks if there are any events in the processing queue.
        /// </summary>
        ///
        /// <returns>True if there are events in the queue, false otherwise.</returns>
        ///
        /// <example>
        /// <code lang="csharp">
        /// if (EventBus.HasQueuedEvents())
        /// {
        ///     int queueSize = EventBus.GetQueueCount();
        ///     UnityEngine.Debug.Log("Events currently in the processing queue: " + queueSize);
        /// }
        /// </code>
        /// </example>
        public static bool HasQueuedEvents() => GetQueueCount() > 0;
    }

    /// <summary>
    /// [DEPRECATED] Wrapper class for backwards compatibility with the instance API.
    /// Use the static <see cref="EventBus"/> methods directly instead.
    /// </summary>
    [Obsolete("The instance API is deprecated.")]
    public class EventBusInstance
    {
        /// <summary>
        /// [DEPRECATED] Sends an event asynchronously.
        /// </summary>
        [Obsolete("Use EventBus.Send() instead of EventBus.Instance.Send().")]
        public void Send<T>(T eventData) => EventBus.Send(eventData);

        /// <summary>
        /// [DEPRECATED] Sends an event synchronously and waits for completion.
        /// </summary>
        [Obsolete("Use EventBus.SendAndWait() instead of EventBus.Instance.SendAndWait().")]
        public void SendAndWait<T>(T eventData) => EventBus.SendAndWait(eventData);

        /// <summary>
        /// [DEPRECATED] Marks an aggregate as ready.
        /// </summary>
        [Obsolete("Use EventBus.AggregateReady() instead of EventBus.Instance.AggregateReady().")]
        public void AggregateReady(Guid aggregateId) => EventBus.AggregateReady(aggregateId);

        /// <inheritdoc cref="EventBus.AggregateReady(Guid, Delegate[])"/>
        [Obsolete("Use EventBus.AggregateReady() instead of EventBus.Instance.AggregateReady().")]
        public void AggregateReady(Guid aggregateId, params Delegate[] handlers) => EventBus.AggregateReady(aggregateId, handlers);

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
        [Obsolete("Use EventBus.GetHandlerCount&lt;T&gt;() instead of EventBus.Instance.GetHandlerCount&lt;T&gt;().")]
        public int GetHandlerCount<T>() => EventBus.GetHandlerCount<T>();

        /// <summary>
        /// [DEPRECATED] Checks if there are handlers for an event type.
        /// </summary>
        [Obsolete("Use EventBus.HasHandlers&lt;T&gt;() instead of EventBus.Instance.HasHandlers&lt;T&gt;().")]
        public bool HasHandlers<T>() => EventBus.HasHandlers<T>();

        /// <summary>
        /// [DEPRECATED] Registers a global handler for events.
        /// </summary>
        [Obsolete("Use EventBus.RegisterGlobalHandler or EventBus.RegisterHandler instead.")]
        public void RegisterStaticHandler<T>(Action<T> handler, IDispatcher? dispatcher = null) => EventBus.RegisterGlobalHandler(handler, dispatcher);

        /// <summary>
        /// [DEPRECATED] Shuts down the event bus.
        /// </summary>
        [Obsolete("Use EventBus.Shutdown() instead of EventBus.Instance.Shutdown().")]
        public void Shutdown() => EventBus.Shutdown();

        /// <summary>
        /// [DEPRECATED] Direct access to handlers is removed; this returns an empty snapshot.
        /// </summary>
        [Obsolete("Direct handlers access is removed; this returns an empty snapshot.")]
        public Dictionary<Type, List<Subscription>> Handlers => new();

        /// <summary>
        /// [DEPRECATED] Direct EventQueue access is removed; this returns an empty queue.
        /// </summary>
        [Obsolete("Direct EventQueue access is removed; this returns an empty queue.")]
        public Queue<QueuedEvent> EventQueue => new();

        /// <summary>
        /// [DEPRECATED] Returns the internal scheduler enqueue lock; holding it pauses Send/queue drain.
        /// </summary>
        [Obsolete("QueueLock now returns the internal scheduler enqueue lock; holding it pauses Send/queue drain.")]
        public object QueueLock => Core.QueueScheduler.EnqueueLock;
    }
}
