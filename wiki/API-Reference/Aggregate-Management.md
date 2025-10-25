# Aggregate Management

Methods for managing event routing and buffering.

## `EventBus.AggregateReady(Guid aggregateId)`

**What it does:** Signals that an aggregate is ready to process its buffered events.

**When to use:** When a game object or system is initialized and ready to handle events.

**Parameters:**
- `aggregateId`: The aggregate ID that is now ready to process events

**Example:**
```csharp
public class PlayerController : MonoBehaviour
{
    private Guid playerId;
    
    void Start()
    {
        playerId = Guid.NewGuid();
        
        // Register handlers
        EventBus.RegisterUnityHandler<PlayerHealthChangedEvent>(
            OnHealthChanged, 
            playerId
        );
        
        // Signal that this player is ready
        EventBus.AggregateReady(playerId);
    }
    
    void OnHealthChanged(PlayerHealthChangedEvent evt)
    {
        // Handle health changes
        UpdateHealthUI(evt.NewHealth);
    }
}
```

**Why use this:** This is essential for proper event ordering. Events sent before an object is ready will be buffered and processed in the correct order once it becomes ready.

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

## `EventBus.GetBufferedEventCount(Guid aggregateId)`

**What it does:** Returns the number of buffered events for a specific aggregate.

**When to use:** For debugging, monitoring, or determining when to process buffered events.

**Parameters:**
- `aggregateId`: The aggregate ID to check

**Returns:** Number of buffered events for the aggregate

**Example:**
```csharp
// Check if player has pending events
int pendingEvents = EventBus.GetBufferedEventCount(playerId);
if (pendingEvents > 0)
{
    Debug.Log($"Player {playerId} has {pendingEvents} pending events");
    EventBus.AggregateReady(playerId);
}
```

**Why use this:** Use this to monitor how many events are waiting for an aggregate to become ready. This is useful for debugging or for determining when to signal readiness.

## `EventBus.GetBufferedAggregateIds()`

**What it does:** Returns all aggregate IDs that have buffered events.

**When to use:** For system monitoring, debugging, or bulk processing.

**Returns:** Collection of aggregate IDs with buffered events

**Example:**
```csharp
// Process all aggregates with buffered events
var aggregatesWithEvents = EventBus.GetBufferedAggregateIds();
foreach (var aggregateId in aggregatesWithEvents)
{
    Debug.Log($"Aggregate {aggregateId} has buffered events");
    EventBus.AggregateReady(aggregateId);
}
```

**Why use this:** Use this to get an overview of which aggregates have pending events. This is useful for system monitoring or for bulk processing of buffered events.

## `EventBus.GetTotalBufferedEventCount()`

**What it does:** Returns the total number of buffered events across all aggregates.

**When to use:** For system health monitoring or analysis.

**Returns:** Total number of buffered events

**Example:**
```csharp
// Monitor system health
int totalBuffered = EventBus.GetTotalBufferedEventCount();
if (totalBuffered > 1000)
{
    Debug.LogWarning($"High number of buffered events: {totalBuffered}");
}
```

**Why use this:** Use this to monitor the overall health of your event system. A high number of buffered events might indicate processing issues or that aggregates are not becoming ready quickly enough.

## `EventBus.HasBufferedEvents()`

**What it does:** Checks if there are any buffered events in the system.

**When to use:** For quick checks before processing or cleanup.

**Returns:** True if there are any buffered events, false otherwise

**Example:**
```csharp
// Check if any events are buffered
if (EventBus.HasBufferedEvents())
{
    Debug.Log("Some events are still buffered");
}
```

**Why use this:** Use this for quick checks to see if there are any buffered events in the system. This is useful for debugging or for determining if the system is ready to proceed.

## `EventBus.HasHandlersForAggregate(Guid aggregateId)`

**What it does:** Checks if there are any handlers registered for a specific aggregate.

**When to use:** For debugging aggregate setup or ensuring proper initialization.

**Parameters:**
- `aggregateId`: The aggregate ID to check

**Returns:** True if handlers are registered for the aggregate, false otherwise

**Example:**
```csharp
// Check if player has handlers
if (!EventBus.HasHandlersForAggregate(playerId))
{
    Debug.LogError($"Player {playerId} has no event handlers registered");
}
```

**Why use this:** Use this to verify that an aggregate has handlers registered before sending events to it. This is useful for debugging or for ensuring proper system initialization.

## `EventBus.GetHandlerCountForAggregate(Guid aggregateId)`

**What it does:** Returns the number of registered handlers for a specific aggregate.

**When to use:** For debugging or monitoring handler registration.

**Parameters:**
- `aggregateId`: The aggregate ID to check

**Returns:** Number of registered handlers for the aggregate

**Example:**
```csharp
// Check handler count for player
int handlerCount = EventBus.GetHandlerCountForAggregate(playerId);
Debug.Log($"Player {playerId} has {handlerCount} handlers registered");
```

**Why use this:** Use this to monitor how many handlers are registered for a specific aggregate. This is useful for debugging or for ensuring that all necessary handlers are properly registered.

## Common Patterns

### 1. Proper Initialization Sequence
```csharp
public class PlayerController : MonoBehaviour
{
    private Guid playerId;
    
    void Start()
    {
        playerId = Guid.NewGuid();
        
        // 1. Register handlers first
        EventBus.RegisterUnityHandler<PlayerEvent>(OnPlayerEvent, playerId);
        
        // 2. Then signal readiness
        EventBus.AggregateReady(playerId);
    }
}
```

### 2. Buffering with Timeouts
```csharp
public class BufferedEventWithTimeout : MonoBehaviour
{
    private float bufferTimeout = 5f;
    
    void Start()
    {
        StartCoroutine(CheckBufferTimeout());
    }
    
    IEnumerator CheckBufferTimeout()
    {
        yield return new WaitForSeconds(bufferTimeout);
        
        // If player still has buffered events, something went wrong
        if (EventBus.GetBufferedEventCount(playerId) > 0)
        {
            Debug.LogWarning($"Player {playerId} still has buffered events after timeout");
        }
    }
}
```

### 3. Bulk Event Processing
```csharp
public class BulkEventProcessor : MonoBehaviour
{
    void ProcessAllBufferedEvents()
    {
        var aggregatesWithEvents = EventBus.GetBufferedAggregateIds();
        
        foreach (var aggregateId in aggregatesWithEvents)
        {
            Debug.Log($"Processing buffered events for aggregate {aggregateId}");
            EventBus.AggregateReady(aggregateId);
        }
    }
}
```

## Best Practices

### 1. Signal Readiness at the Right Time
```csharp
// Good: Signal readiness after handlers are registered
void Start()
{
    RegisterEventHandlers();
    EventBus.AggregateReady(aggregateId);
}

// Bad: Signaling before handlers are ready
void Awake()
{
    EventBus.AggregateReady(aggregateId); // Too early!
    RegisterEventHandlers();
}
```

### 2. Handle Buffering Gracefully
```csharp
public class RobustPlayerController : MonoBehaviour
{
    void Start()
    {
        // Register handlers
        RegisterEventHandlers();
        
        // Check if there are buffered events
        int bufferedCount = EventBus.GetBufferedEventCount(PlayerId);
        if (bufferedCount > 0)
        {
            Debug.Log($"Processing {bufferedCount} buffered events");
        }
        
        // Signal readiness
        EventBus.AggregateReady(PlayerId);
    }
}
```

### 3. Clean Up When Done
```csharp
void OnDestroy()
{
    // Clear buffered events when object is destroyed
    EventBus.ClearEventsForAggregate(PlayerId);
}
```

## Common Mistakes

### ❌ Don't: Forget to call AggregateReady
```csharp
// WRONG - Events will be buffered forever
void Start()
{
    EventBus.RegisterUnityHandler<PlayerEvent>(OnPlayerEvent, PlayerId);
    // Forgot to call EventBus.AggregateReady(PlayerId);
}
```

### ❌ Don't: Call AggregateReady too early
```csharp
// WRONG - Handlers not registered yet
void Awake()
{
    EventBus.AggregateReady(PlayerId); // Too early!
    // Handlers registered later in Start()
}
```

### ✅ Do: Proper initialization sequence
```csharp
// CORRECT - Proper sequence
void Start()
{
    // 1. Register handlers
    EventBus.RegisterUnityHandler<PlayerEvent>(OnPlayerEvent, PlayerId);
    
    // 2. Signal readiness
    EventBus.AggregateReady(PlayerId);
}
```

## System Considerations

1. **Monitor buffered events** - Use `GetTotalBufferedEventCount()` to monitor system health
2. **Process buffered events promptly** - Don't let them accumulate
3. **Use appropriate cleanup methods** - Choose the right method for your use case
4. **Profile aggregate management** - Monitor the execution time of these operations
5. **Test edge cases** - Ensure proper behavior in all scenarios