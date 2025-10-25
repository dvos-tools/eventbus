# Event Buffering

Event buffering is a powerful feature that ensures events are never lost, even when sent to objects that aren't ready to receive them yet. This is crucial for maintaining proper event ordering and system reliability.

## What is Event Buffering?

Event buffering automatically stores routed events when the target aggregate doesn't have any handlers registered yet. Once the aggregate becomes ready, all buffered events are processed in the correct order.

## Why Buffering Matters

In complex systems, objects are often created and initialized at different times:

```csharp
// This could happen in any order:
// 1. Player takes damage (event sent)
// 2. Player object is created
// 3. Player handlers are registered
// 4. Player becomes ready

// Without buffering: Event is lost!
// With buffering: Event is stored and processed when ready
```

## How Buffering Works

### 1. Event Sent to Non-Ready Aggregate
```csharp
// Player doesn't exist yet, but we send an event
EventBus.Send(new PlayerHealthChangedEvent 
{ 
    AggregateId = playerId,
    NewHealth = 80,
    MaxHealth = 100
});
// Event is automatically buffered
```

### 2. Aggregate Becomes Ready
```csharp
public class PlayerController : MonoBehaviour
{
    public Guid PlayerId { get; private set; }
    
    void Start()
    {
        PlayerId = Guid.NewGuid();
        
        // Register handlers
        EventBus.RegisterUnityHandler<PlayerHealthChangedEvent>(OnHealthChanged, PlayerId);
        
        // Signal readiness - buffered events are now processed
        EventBus.AggregateReady(PlayerId);
    }
    
    void OnHealthChanged(PlayerHealthChangedEvent evt)
    {
        // This will receive the buffered event!
        UpdateHealthUI(evt.NewHealth);
    }
}
```

### 3. Buffered Events Processed
All buffered events for the aggregate are processed in the order they were sent.

## Real-World Example: Game Initialization

Let's see how buffering helps with game initialization:

### Step 1: Define Events
```csharp
public class PlayerSpawnedEvent : IRoutableEvent
{
    public Guid AggregateId { get; set; }
    public Vector3 Position { get; set; }
    public string PlayerName { get; set; }
}

public class PlayerHealthChangedEvent : IRoutableEvent
{
    public Guid AggregateId { get; set; }
    public int NewHealth { get; set; }
    public int MaxHealth { get; set; }
}

public class PlayerMovedEvent : IRoutableEvent
{
    public Guid AggregateId { get; set; }
    public Vector3 Position { get; set; }
}
```

### Step 2: Create Game Manager
```csharp
public class GameManager : MonoBehaviour
{
    private Guid playerId;
    
    void Start()
    {
        // Generate player ID early
        playerId = Guid.NewGuid();
        
        // Send events before player is ready
        SendInitialEvents();
        
        // Create player after a delay (simulating async loading)
        StartCoroutine(CreatePlayerAfterDelay());
    }
    
    void SendInitialEvents()
    {
        // These events will be buffered until player is ready
        EventBus.Send(new PlayerSpawnedEvent
        {
            AggregateId = playerId,
            Position = new Vector3(0, 0, 0),
            PlayerName = "Player1"
        });
        
        EventBus.Send(new PlayerHealthChangedEvent
        {
            AggregateId = playerId,
            NewHealth = 100,
            MaxHealth = 100
        });
        
        EventBus.Send(new PlayerMovedEvent
        {
            AggregateId = playerId,
            Position = new Vector3(10, 0, 5)
        });
    }
    
    IEnumerator CreatePlayerAfterDelay()
    {
        // Simulate loading delay
        yield return new WaitForSeconds(2f);
        
        // Create player object
        var playerPrefab = Resources.Load<GameObject>("Player");
        var player = Instantiate(playerPrefab);
        var playerController = player.GetComponent<PlayerController>();
        playerController.Initialize(playerId);
    }
}
```

### Step 3: Create Player Controller
```csharp
public class PlayerController : MonoBehaviour
{
    public Guid PlayerId { get; private set; }
    
    public void Initialize(Guid playerId)
    {
        PlayerId = playerId;
        
        // Register handlers
        EventBus.RegisterUnityHandler<PlayerSpawnedEvent>(OnPlayerSpawned, PlayerId);
        EventBus.RegisterUnityHandler<PlayerHealthChangedEvent>(OnHealthChanged, PlayerId);
        EventBus.RegisterUnityHandler<PlayerMovedEvent>(OnMoved, PlayerId);
        
        // Signal readiness - all buffered events will be processed
        EventBus.AggregateReady(PlayerId);
    }
    
    void OnPlayerSpawned(PlayerSpawnedEvent evt)
    {
        // This will receive the buffered spawn event
        transform.position = evt.Position;
        name = evt.PlayerName;
        Debug.Log($"Player {evt.PlayerName} spawned at {evt.Position}");
    }
    
    void OnHealthChanged(PlayerHealthChangedEvent evt)
    {
        // This will receive the buffered health event
        UpdateHealthUI(evt.NewHealth, evt.MaxHealth);
        Debug.Log($"Player health set to {evt.NewHealth}/{evt.MaxHealth}");
    }
    
    void OnMoved(PlayerMovedEvent evt)
    {
        // This will receive the buffered movement event
        transform.position = evt.Position;
        Debug.Log($"Player moved to {evt.Position}");
    }
    
    void UpdateHealthUI(int current, int max)
    {
        // Update health UI
        var healthBar = GetComponentInChildren<Slider>();
        if (healthBar != null)
        {
            healthBar.value = (float)current / max;
        }
    }
}
```

## Buffering Management

### Check Buffered Events
```csharp
// Check if an aggregate has buffered events
int bufferedCount = EventBus.GetBufferedEventCount(playerId);
if (bufferedCount > 0)
{
    Debug.Log($"Player {playerId} has {bufferedCount} buffered events");
}

// Check if any aggregates have buffered events
if (EventBus.HasBufferedEvents())
{
    Debug.Log("Some events are still buffered");
}

// Get all aggregates with buffered events
var aggregatesWithEvents = EventBus.GetBufferedAggregateIds();
foreach (var aggregateId in aggregatesWithEvents)
{
    Debug.Log($"Aggregate {aggregateId} has buffered events");
}
```

### Clear Buffered Events
```csharp
// Clear buffered events for a specific aggregate
EventBus.ClearEventsForAggregate(playerId);

// Reset an aggregate completely
EventBus.ResetAggregate(playerId);
```

## Advanced Buffering Patterns

### 1. Conditional Buffering
```csharp
public class ConditionalEventManager : MonoBehaviour
{
    void Start()
    {
        // Only buffer events if player is not ready
        if (!EventBus.HasHandlersForAggregate(playerId))
        {
            // Buffer events
            EventBus.Send(new PlayerEvent { AggregateId = playerId });
        }
        else
        {
            // Send immediately
            EventBus.Send(new PlayerEvent { AggregateId = playerId });
        }
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
            // Handle timeout scenario
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
// Good: Signal readiness after all handlers are registered
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

## Common Pitfalls

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

## Event Pooling (Side Effect)

**⚠️ Important Note:** Event pooling is not an officially supported feature. It's a side effect of the buffering system that happens when you send events to aggregates that aren't ready yet.

### How Event Pooling Works

When you send events to an aggregate that doesn't have handlers registered yet, those events get stored in memory until the aggregate becomes ready:

```csharp
// Send events before aggregate is ready - they get "pooled"
EventBus.Send(new PlayerHealthChangedEvent { AggregateId = playerId, NewHealth = 80 });
EventBus.Send(new PlayerMovedEvent { AggregateId = playerId, Position = new Vector3(10, 0, 5) });
EventBus.Send(new PlayerScoredEvent { AggregateId = playerId, Points = 100 });

// These events are now pooled in memory until AggregateReady() is called
```

### Why This Happens

Event pooling occurs because:
1. **Events are sent** to an aggregate ID
2. **No handlers are registered** for that aggregate yet
3. **Events get buffered** in memory automatically
4. **Memory accumulates** until `AggregateReady()` is called

### ⚠️ Don't Abuse Event Pooling

**This is NOT a feature you should rely on or abuse:**

```csharp
// ❌ BAD - Don't use this as a pooling mechanism
void Update()
{
    // Sending hundreds of events to unready aggregates
    for (int i = 0; i < 100; i++)
    {
        EventBus.Send(new PlayerEvent { AggregateId = unreadyPlayerId, Data = i });
    }
    // This will consume a lot of memory!
}

// ✅ GOOD - Send events normally
void Update()
{
    // Only send events when needed
    if (shouldSendEvent)
    {
        EventBus.Send(new PlayerEvent { AggregateId = readyPlayerId, Data = currentData });
    }
}
```

### Memory Considerations

Event pooling can lead to:
- **Memory bloat** if too many events are sent to unready aggregates
- **Processing issues** when handling large numbers of buffered events
- **Unexpected behavior** if you rely on this side effect

### Best Practices for Event Pooling

1. **Don't rely on it** - It's a side effect, not a feature
2. **Call AggregateReady() promptly** - Don't let events accumulate unnecessarily
3. **Monitor buffered events** - Use `GetTotalBufferedEventCount()` to check
4. **Clear events when appropriate** - Use `ClearEventsForAggregate()` if needed

```csharp
// Good practice: Monitor and manage buffered events
void Update()
{
    int totalBuffered = EventBus.GetTotalBufferedEventCount();
    if (totalBuffered > 1000)
    {
        Debug.LogWarning($"High number of buffered events: {totalBuffered}");
        // Consider calling AggregateReady() for unready aggregates
    }
}
```

### When Event Pooling is Acceptable

Event pooling is acceptable when:
- **Temporary buffering** during object initialization
- **Short delays** between event sending and aggregate readiness
- **Small numbers** of events being buffered

```csharp
// Acceptable: Temporary buffering during initialization
void Start()
{
    // Send a few events during initialization
    EventBus.Send(new PlayerSpawnedEvent { AggregateId = playerId });
    EventBus.Send(new PlayerHealthChangedEvent { AggregateId = playerId, NewHealth = 100 });
    
    // Register handlers and signal readiness quickly
    EventBus.RegisterUnityHandler<PlayerEvent>(OnPlayerEvent, playerId);
    EventBus.AggregateReady(playerId); // Events are processed immediately
}
```

## Next Steps

Now that you understand event buffering and pooling, let's learn about best practices:

**➡️ Continue to [08-Best Practices](08-Best-Practices)**

---

## Quick Reference

| Method | Purpose | When to Use |
|--------|---------|-------------|
| `EventBus.AggregateReady(aggregateId)` | Signal aggregate is ready | After handlers are registered |
| `EventBus.GetBufferedEventCount(aggregateId)` | Check buffered events | For monitoring/debugging |
| `EventBus.ClearEventsForAggregate(aggregateId)` | Clear buffered events | When discarding events |
| `EventBus.ResetAggregate(aggregateId)` | Reset aggregate completely | When destroying objects |
| `EventBus.GetTotalBufferedEventCount()` | Check total buffered events | For monitoring pooling |