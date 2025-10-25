# Handler Management

Methods for cleaning up and managing event handlers.

## `EventBus.DisposeHandlers<T>()`

**What it does:** Removes all handlers for a specific event type and clears all its queued events.

**When to use:** When you want to stop listening to a specific event type.

**Parameters:**
- `T`: The type of event to dispose handlers for

**Example:**
```csharp
// Stop listening to player events
EventBus.DisposeHandlers<PlayerDiedEvent>();

// Stop listening to health events
EventBus.DisposeHandlers<PlayerHealthChangedEvent>();
```

**Why use this:** Use this when you want to stop listening to a specific event type. This is useful when switching scenes, changing game modes, or when certain systems are no longer needed.

## `EventBus.DisposeAllHandlers()`

**What it does:** Removes all handlers for all event types and clears all events.

**When to use:** For cleanup when shutting down systems or switching scenes.

**Example:**
```csharp
// Clean up all event handlers when changing scenes
EventBus.DisposeAllHandlers();

// Clean up when shutting down
void OnApplicationQuit()
{
    EventBus.DisposeAllHandlers();
}
```

**Why use this:** Use this for complete cleanup when you want to reset the entire event system. This is useful for scene transitions, game restarts, or when shutting down the application.

## `EventBus.DisposeHandlersForAggregate<T>(Guid aggregateId)`

**What it does:** Disposes handlers for a specific event type and aggregate ID, and clears all its events.

**When to use:** When you want to stop listening to events for a specific aggregate.

**Parameters:**
- `T`: The type of event to dispose handlers for
- `aggregateId`: The aggregate ID to dispose handlers for

**Example:**
```csharp
// Stop listening to events for a specific player
EventBus.DisposeHandlersForAggregate<PlayerHealthChangedEvent>(playerId);

// Stop listening to events for a specific system
EventBus.DisposeHandlersForAggregate<InventoryEvent>(inventorySystemId);
```

**Why use this:** Use this when you want to stop listening to events for a specific aggregate (like a player or system) while keeping handlers for other aggregates active.

## `EventBus.DisposeHandlerFromAggregate<T>(Action<T> handler, Guid aggregateId)`

**What it does:** Disposes a specific handler from an aggregate by reference and clears all its events.

**When to use:** When you want to remove a specific handler while keeping others.

**Parameters:**
- `handler`: The handler function to dispose
- `aggregateId`: The aggregate ID to dispose the handler from

**Example:**
```csharp
// Remove a specific handler
EventBus.DisposeHandlerFromAggregate<PlayerEvent>(OnPlayerEvent, playerId);

// Remove a specific UI handler
EventBus.DisposeHandlerFromAggregate<HealthEvent>(UpdateHealthUI, playerId);
```

**Why use this:** Use this when you want to remove a specific handler while keeping other handlers for the same event type and aggregate active.

## `EventBus.ClearEventsForAggregate(Guid aggregateId)`

**What it does:** Clears all queued and buffered events for a specific aggregate ID.

**When to use:** When you want to discard pending events for an aggregate.

**Parameters:**
- `aggregateId`: The aggregate ID to clear events for

**Example:**
```csharp
// Player disconnected - clear their pending events
EventBus.ClearEventsForAggregate(playerId);

// System reset - clear all pending events
EventBus.ClearEventsForAggregate(systemId);
```

**Why use this:** Use this when you want to discard pending events for a specific aggregate without removing the handlers. This is useful when an aggregate is temporarily unavailable or when you want to reset its state.

## `EventBus.ResetAggregate(Guid aggregateId)`

**What it does:** Removes all handlers and clears all events for an aggregate.

**When to use:** When completely resetting an aggregate (e.g., player respawn, object destruction).

**Parameters:**
- `aggregateId`: The aggregate ID to reset

**Example:**
```csharp
// Player respawned - reset everything
EventBus.ResetAggregate(playerId);

// Object destroyed - clean up completely
EventBus.ResetAggregate(objectId);
```

**Why use this:** Use this when you want to completely reset an aggregate. This removes all handlers and clears all events, effectively starting fresh for that aggregate.

## `EventBus.ClearAll()`

**What it does:** Clears all handlers, buffered events, and queued events.

**When to use:** For test cleanup or complete system reset.

**Example:**
```csharp
// Clean up after tests
[Test]
public void TestEventHandling()
{
    // Test code...
    
    // Clean up
    EventBus.ClearAll();
}

// Complete system reset
EventBus.ClearAll();
```

**Why use this:** Use this for complete system reset. This is useful for testing, debugging, or when you want to start fresh with the event system.

## Cleanup Patterns

### Automatic Cleanup
```csharp
public class PlayerController : MonoBehaviour
{
    private Guid playerId;
    
    void Start()
    {
        playerId = Guid.NewGuid();
        EventBus.RegisterUnityHandler<PlayerEvent>(OnPlayerEvent, playerId);
    }
    
    void OnDestroy()
    {
        // Automatic cleanup when object is destroyed
        EventBus.ResetAggregate(playerId);
    }
}
```

### Manual Cleanup
```csharp
public class GameManager : MonoBehaviour
{
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // Pause game - clear some events
            EventBus.ClearEventsForAggregate(playerId);
        }
    }
    
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            // Lost focus - clean up
            EventBus.DisposeAllHandlers();
        }
    }
}
```

### Selective Cleanup
```csharp
public class UIManager : MonoBehaviour
{
    void OnDisable()
    {
        // Only clean up UI-related handlers
        EventBus.DisposeHandlers<UIEvent>();
        EventBus.DisposeHandlers<HealthEvent>();
    }
}
```

## Best Practices

### 1. Clean Up When Done
```csharp
// Good: Clean up when object is destroyed
void OnDestroy()
{
    EventBus.ResetAggregate(playerId);
}

// Bad: No cleanup - memory leak
void OnDestroy()
{
    // No cleanup - handlers remain registered
}
```

### 2. Use Appropriate Cleanup Methods
```csharp
// Good: Use specific cleanup methods
EventBus.DisposeHandlers<PlayerEvent>(); // Only player events
EventBus.ClearEventsForAggregate(playerId); // Only this player

// Bad: Over-cleanup
EventBus.ClearAll(); // Clears everything unnecessarily
```

### 3. Clean Up in the Right Order
```csharp
// Good: Clean up in proper order
void OnDestroy()
{
    // 1. Clear events first
    EventBus.ClearEventsForAggregate(playerId);
    
    // 2. Then dispose handlers
    EventBus.DisposeHandlersForAggregate<PlayerEvent>(playerId);
}

// Bad: Wrong order
void OnDestroy()
{
    EventBus.DisposeHandlersForAggregate<PlayerEvent>(playerId);
    EventBus.ClearEventsForAggregate(playerId); // Too late
}
```

## Common Mistakes

### ❌ Don't: Forget to clean up
```csharp
// WRONG - No cleanup
void Start()
{
    EventBus.RegisterUnityHandler<PlayerEvent>(OnPlayerEvent);
    // No cleanup in OnDestroy()
}
```

### ❌ Don't: Over-cleanup
```csharp
// WRONG - Over-cleanup
void OnDestroy()
{
    EventBus.ClearAll(); // Clears everything, not just this object
}
```

### ✅ Do: Appropriate cleanup
```csharp
// CORRECT - Appropriate cleanup
void OnDestroy()
{
    EventBus.ResetAggregate(playerId); // Only this player
}
```

## System Considerations

1. **Clean up promptly** - Don't leave handlers registered unnecessarily
2. **Use specific methods** - Avoid `ClearAll()` when you only need to clean up specific handlers
3. **Monitor handler count** - Use `GetHandlerCount<T>()` to monitor
4. **Profile cleanup** - Make sure cleanup doesn't cause processing issues
5. **Test cleanup** - Ensure handlers are properly disposed in all scenarios