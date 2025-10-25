# Event Subscription

Methods for subscribing to events and registering handlers.

## `EventBus.RegisterHandler<T>(Action<T> handler, Guid aggregateId = default, IDispatcher? dispatcher = null)`

**What it does:** Subscribes to receive events of a specific type.

**When to use:** The general-purpose subscription method for most use cases.

**Parameters:**
- `handler` (Action<T>): The function to call when this event is received
- `aggregateId` (Guid): Game object ID for routed events, or Guid.Empty for global handlers
- `dispatcher` (IDispatcher?): Which thread to run the handler on (defaults to background thread)

**Example:**
```csharp
// Register a global handler (receives all events of this type)
EventBus.RegisterHandler<PlayerDiedEvent>(OnPlayerDied);

// Register a routed handler (only receives events for specific player)
EventBus.RegisterHandler<PlayerHealthChangedEvent>(OnHealthChanged, playerId);

// Register with specific dispatcher
EventBus.RegisterHandler<HeavyComputationEvent>(ProcessData, 
    Guid.Empty, 
    new ThreadPoolDispatcher());
```

**Why use this:** This is the most flexible subscription method. You can specify routing, threading, and other options as needed.

## `EventBus.RegisterUnityHandler<T>(Action<T> handler, Guid aggregateId = default)`

**What it does:** Subscribes to events with handlers that run on Unity's main thread.

**When to use:** When your handler needs to access Unity APIs (GameObjects, Components, UI, etc.).

**Parameters:**
- `handler` (Action<T>): The function to call when this event is received
- `aggregateId` (Guid): Aggregate ID for routed events, or Guid.Empty for global handlers

**Example:**
```csharp
// Update UI when player health changes
EventBus.RegisterUnityHandler<PlayerHealthChangedEvent>(UpdateHealthBar);

void UpdateHealthBar(PlayerHealthChangedEvent evt)
{
    // Safe to access Unity UI components
    healthBar.fillAmount = evt.NewHealth / 100f;
    healthText.text = $"Health: {evt.NewHealth}";
    
    // Can access GameObjects and Components
    var player = GameObject.FindWithTag("Player");
    if (player != null)
    {
        player.GetComponent<PlayerController>().UpdateHealth(evt.NewHealth);
    }
}
```

**Why use this:** Unity APIs must be called from the main thread. This method ensures your handlers can safely interact with Unity objects without causing threading issues.

**Execution:** Handlers execute one per frame to maintain smooth gameplay.

## `EventBus.RegisterBackgroundHandler<T>(Action<T> handler, Guid aggregateId = default)`

**⚠️ NOT SUPPORTED** - This method is not currently supported. Use `RegisterUnityHandler` instead.

## `EventBus.RegisterImmediateHandler<T>(Action<T> handler, Guid aggregateId = default)`

**⚠️ NOT SUPPORTED** - This method is not currently supported. Use `RegisterUnityHandler` instead.

## Convenience Methods

### `EventBus.RegisterGlobalHandler<T>(Action<T> handler, IDispatcher? dispatcher = null)`

**What it does:** Registers a global handler that receives all events of the specified type (equivalent to `RegisterHandler` with `Guid.Empty`).

**When to use:** For non-routed events where you want to receive all instances.

**Example:**
```csharp
// Listen to all game state changes
EventBus.RegisterGlobalHandler<GameStateChangedEvent>(OnGameStateChanged);
```

### `EventBus.RegisterRoutedHandler<T>(Action<T> handler, Guid aggregateId, IDispatcher? dispatcher = null)`

**What it does:** Registers a handler specifically for routed events with a required aggregate ID.

**When to use:** For routed events where you want to ensure the aggregate ID is provided.

**Example:**
```csharp
// Register for specific player events only
EventBus.RegisterRoutedHandler<PlayerEvent>(HandlePlayerEvent, playerId);
```

## Handler Registration Patterns

### Global Handlers
```csharp
// Receives all events of this type
EventBus.RegisterUnityHandler<GameStartedEvent>(OnGameStarted);
```

### Routed Handlers
```csharp
// Only receives events for specific aggregate
EventBus.RegisterUnityHandler<PlayerEvent>(OnPlayerEvent, playerId);
```

### Mixed Handlers
```csharp
// Global handler for all players
EventBus.RegisterUnityHandler<PlayerDiedEvent>(OnAnyPlayerDied);

// Specific handler for one player
EventBus.RegisterUnityHandler<PlayerDiedEvent>(OnMyPlayerDied, myPlayerId);
```

## Threading Considerations

### Unity Main Thread (Only Supported Option)
```csharp
// Safe for Unity API access - this is the only supported option
EventBus.RegisterUnityHandler<PlayerEvent>(OnPlayerEvent);
```

### Unsupported Dispatchers
```csharp
// These are NOT supported:
// EventBus.RegisterBackgroundHandler<DataEvent>(ProcessData); // Not supported
// EventBus.RegisterImmediateHandler<ValidationEvent>(ValidateInput); // Not supported
```

## Best Practices

1. **Choose the right dispatcher** for your use case
2. **Register handlers early** - Preferably in `Start()` or `Awake()`
3. **Use descriptive handler names** - Makes debugging easier
4. **Handle errors gracefully** - Don't let handler errors crash your system
5. **Clean up handlers** - Dispose them when no longer needed

## Common Mistakes

### ❌ Don't: Use unsupported dispatchers
```csharp
// WRONG - These are not supported
EventBus.RegisterBackgroundHandler<PlayerEvent>(OnPlayerEvent); // Not supported
EventBus.RegisterImmediateHandler<PlayerEvent>(OnPlayerEvent); // Not supported
```

### ❌ Don't: Register handlers in Update()
```csharp
// WRONG - This registers handlers every frame
void Update()
{
    EventBus.RegisterUnityHandler<PlayerEvent>(OnPlayerEvent);
}
```

### ✅ Do: Use UnityDispatcher for all event handling
```csharp
// CORRECT - Use UnityDispatcher for all events
void Start()
{
    EventBus.RegisterUnityHandler<PlayerEvent>(OnPlayerEvent);
    EventBus.RegisterUnityHandler<UIEvent>(OnUIEvent);
    EventBus.RegisterUnityHandler<GameEvent>(OnGameEvent);
}
```

## Optimization Tips

1. **Register handlers once** - Don't register them repeatedly
2. **Use UnityDispatcher** - It's the only supported dispatcher
3. **Keep handlers lightweight** - Avoid heavy computations that could cause frame drops
4. **Use coroutines for heavy work** - Move CPU-intensive tasks to coroutines
5. **Monitor handler count** - Use `GetHandlerCount<T>()` to monitor
6. **Profile handler execution** - Use stopwatch to measure execution time