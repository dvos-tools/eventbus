# Best Practices

This guide covers professional patterns and recommendations for building robust, maintainable event-driven systems with Unity EventBus.

## Event Design

### 1. Keep Events Simple and Focused
```csharp
// Good: Simple, focused event
public class PlayerHealthChangedEvent
{
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public int HealthChange { get; set; }
}

// Bad: Complex, unfocused event
public class PlayerEvent
{
    public string PlayerName { get; set; }
    public int Health { get; set; }
    public Vector3 Position { get; set; }
    public int Score { get; set; }
    public bool IsAlive { get; set; }
    public string Action { get; set; }
    public object Data { get; set; }
}
```

### 2. Use Descriptive Names
```csharp
// Good: Clear, descriptive names
public class PlayerDiedEvent { }
public class InventoryItemAddedEvent { }
public class GameStateChangedEvent { }

// Bad: Vague, unclear names
public class Event1 { }
public class PlayerEvent { }
public class StateEvent { }
```

### 3. Make Events Immutable
```csharp
// Good: Immutable event
public class PlayerHealthChangedEvent
{
    public int CurrentHealth { get; }
    public int MaxHealth { get; }
    public int HealthChange { get; }
    
    public PlayerHealthChangedEvent(int currentHealth, int maxHealth, int healthChange)
    {
        CurrentHealth = currentHealth;
        MaxHealth = maxHealth;
        HealthChange = healthChange;
    }
}

// Bad: Mutable event
public class PlayerHealthChangedEvent
{
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public int HealthChange { get; set; }
}
```


## Handler Design

### 1. Keep Handlers Focused
```csharp
// Good: Focused handler
void OnPlayerHealthChanged(PlayerHealthChangedEvent evt)
{
    UpdateHealthUI(evt.CurrentHealth, evt.MaxHealth);
}

// Bad: Unfocused handler
void OnPlayerHealthChanged(PlayerHealthChangedEvent evt)
{
    UpdateHealthUI(evt.CurrentHealth, evt.MaxHealth);
    UpdateScoreUI();
    PlaySound();
    SaveToFile();
    SendToServer();
    UpdateAI();
}
```

### 2. Handle Errors Gracefully
```csharp
// Good: Error handling
void OnPlayerHealthChanged(PlayerHealthChangedEvent evt)
{
    try
    {
        UpdateHealthUI(evt.CurrentHealth, evt.MaxHealth);
    }
    catch (Exception ex)
    {
        Debug.LogError($"Failed to update health UI: {ex.Message}");
        // Continue execution - don't crash the system
    }
}

// Bad: No error handling
void OnPlayerHealthChanged(PlayerHealthChangedEvent evt)
{
    UpdateHealthUI(evt.CurrentHealth, evt.MaxHealth); // Could crash
}
```



## EventBus Optimization

### 1. Minimize Event Frequency
```csharp
// Good: Batch events
public class PlayerController : MonoBehaviour
{
    private List<PlayerActionEvent> pendingActions = new();
    
    void Update()
    {
        // Collect actions
        if (Input.GetKeyDown(KeyCode.Space))
        {
            pendingActions.Add(new PlayerActionEvent { Action = "Jump" });
        }
        
        // Send batch every few frames
        if (Time.frameCount % 5 == 0 && pendingActions.Count > 0)
        {
            EventBus.Send(new PlayerActionBatchEvent { Actions = pendingActions });
            pendingActions.Clear();
        }
    }
}

// Bad: Send events every frame
void Update()
{
    if (Input.GetKey(KeyCode.W))
    {
        EventBus.Send(new PlayerMovedEvent { Position = transform.position });
    }
}
```

### 2. Avoid Unnecessary Event Allocations
```csharp
// Bad: Creating new event objects every frame
void Update()
{
    // This creates a new event object every frame
    EventBus.Send(new PlayerMovedEvent 
    { 
        Position = transform.position,
        Rotation = transform.rotation,
        Velocity = rigidbody.velocity
    });
}

// Good: Reuse event objects when possible
public class PlayerController : MonoBehaviour
{
    private PlayerMovedEvent reusableEvent = new();
    
    void Update()
    {
        // Reuse the same event object
        reusableEvent.Position = transform.position;
        reusableEvent.Rotation = transform.rotation;
        reusableEvent.Velocity = rigidbody.velocity;
        
        EventBus.Send(reusableEvent);
    }
}

// Better: Only send events when data actually changes
public class PlayerController : MonoBehaviour
{
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    
    void Update()
    {
        // Only send event if position actually changed
        if (transform.position != lastPosition)
        {
            EventBus.Send(new PlayerMovedEvent 
            { 
                Position = transform.position,
                OldPosition = lastPosition
            });
            lastPosition = transform.position;
        }
    }
}
```

### 3. Avoid Event Pooling Abuse
```csharp
// Bad: Abusing event buffering as a pooling mechanism
void Update()
{
    // Don't send hundreds of events to unready aggregates
    for (int i = 0; i < 100; i++)
    {
        EventBus.Send(new PlayerEvent { AggregateId = unreadyPlayerId, Data = i });
    }
    // This will consume excessive memory!
}

// Good: Send events only when needed and to ready aggregates
void Update()
{
    if (shouldSendEvent && EventBus.HasHandlersForAggregate(playerId))
    {
        EventBus.Send(new PlayerEvent { AggregateId = playerId, Data = currentData });
    }
}
```

### 3. Profile Your Event Handlers
```csharp
// Good: Profiling
void OnPlayerMoved(PlayerMovedEvent evt)
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    // Your handler code
    UpdatePosition(evt.Position);
    
    stopwatch.Stop();
    if (stopwatch.ElapsedMilliseconds > 5)
    {
        Debug.LogWarning($"Slow handler: {stopwatch.ElapsedMilliseconds}ms");
    }
}
```

## EventBus Memory Management

### 1. Clean Up Handlers
```csharp
// Good: Clean up handlers when done
public class PlayerController : MonoBehaviour
{
    void Start()
    {
        EventBus.RegisterUnityHandler<PlayerEvent>(OnPlayerEvent, PlayerId);
    }
    
    void OnDestroy()
    {
        // Clean up when object is destroyed
        EventBus.DisposeHandlersForAggregate<PlayerEvent>(PlayerId);
    }
}

// Bad: No cleanup - memory leak
public class PlayerController : MonoBehaviour
{
    void Start()
    {
        EventBus.RegisterUnityHandler<PlayerEvent>(OnPlayerEvent, PlayerId);
        // No cleanup - handlers remain registered!
    }
}
```

### 2. Clear Events When Appropriate
```csharp
// Good: Clear events when not needed
public class PlayerController : MonoBehaviour
{
    void OnDisable()
    {
        // Clear buffered events when player becomes inactive
        EventBus.ClearEventsForAggregate(PlayerId);
    }
    
    void OnDestroy()
    {
        // Complete cleanup when destroyed
        EventBus.ResetAggregate(PlayerId);
    }
}
```

## EventBus Testing

### 1. Test Event Handlers
```csharp
[Test]
public void TestPlayerHealthChanged()
{
    // Arrange
    var eventReceived = false;
    var receivedHealth = 0;
    
    EventBus.RegisterUnityHandler<PlayerHealthChangedEvent>(evt =>
    {
        eventReceived = true;
        receivedHealth = evt.CurrentHealth;
    });
    
    // Act
    EventBus.Send(new PlayerHealthChangedEvent { CurrentHealth = 80 });
    
    // Assert
    Assert.IsTrue(eventReceived);
    Assert.AreEqual(80, receivedHealth);
    
    // Cleanup
    EventBus.ClearAll();
}
```

### 2. Test Event Routing
```csharp
[Test]
public void TestEventRouting()
{
    // Arrange
    var playerId = Guid.NewGuid();
    var globalReceived = false;
    var routedReceived = false;
    
    EventBus.RegisterUnityHandler<PlayerEvent>(evt => globalReceived = true);
    EventBus.RegisterUnityHandler<PlayerEvent>(evt => routedReceived = true, playerId);
    
    // Act
    EventBus.Send(new PlayerEvent { AggregateId = playerId });
    
    // Assert
    Assert.IsTrue(globalReceived); // Global handler receives all events
    Assert.IsTrue(routedReceived); // Routed handler receives specific events
    
    // Cleanup
    EventBus.ClearAll();
}
```

## EventBus Debugging

### 1. Enable EventBus Logging
```csharp
// Enable logging for debugging
EventBusLogger.EnableLogging = true;

// Your event code...

// Disable for production
EventBusLogger.EnableLogging = false;
```

### 2. Monitor EventBus State
```csharp
// Good: Monitor EventBus health
public class EventBusMonitor : MonoBehaviour
{
    void Update()
    {
        // Check queue size
        int queueSize = EventBus.GetQueueCount();
        if (queueSize > 100)
        {
            Debug.LogWarning($"High event queue size: {queueSize}");
        }
        
        // Check buffered events
        int bufferedCount = EventBus.GetTotalBufferedEventCount();
        if (bufferedCount > 1000)
        {
            Debug.LogWarning($"High buffered event count: {bufferedCount}");
        }
    }
}
```

### 3. Debug Event Flow
```csharp
// Good: Debug event flow
void OnPlayerHealthChanged(PlayerHealthChangedEvent evt)
{
    Debug.Log($"EventBus: Health changed to {evt.CurrentHealth}");
    UpdateHealthUI(evt.CurrentHealth, evt.MaxHealth);
}
```

## EventBus Anti-Patterns

### ❌ Don't: Send Events in Loops
```csharp
// Bad: Sending events in loops
for (int i = 0; i < 1000; i++)
{
    EventBus.Send(new PlayerEvent { Index = i });
}
```

### ❌ Don't: Send Events Every Frame
```csharp
// Bad: Sending events every frame
void Update()
{
    EventBus.Send(new PlayerMovedEvent { Position = transform.position });
}
```

### ❌ Don't: Forget to Call AggregateReady
```csharp
// Bad: Events will be buffered forever
void Start()
{
    EventBus.RegisterUnityHandler<PlayerEvent>(OnPlayerEvent, PlayerId);
    // Forgot to call EventBus.AggregateReady(PlayerId);
}
```

### ❌ Don't: Use Events for Direct Communication
```csharp
// Bad: Using events for direct communication
EventBus.Send(new GetPlayerHealthEvent { PlayerId = playerId });
// Then waiting for response...

// Good: Use direct method calls for simple communication
int health = playerController.GetHealth();
```

## Next Steps

Now that you understand EventBus best practices, let's explore common patterns:

**➡️ Continue to [09-Common Patterns](09-Common-Patterns)**

---

## Quick Reference

| EventBus Best Practice | Description | Example |
|------------------------|-------------|---------|
| **Simple Events** | Keep events focused and simple | `PlayerHealthChangedEvent` |
| **Descriptive Names** | Use clear, descriptive names | `PlayerDiedEvent` not `Event1` |
| **Immutable Events** | Make events immutable | Use `get` only properties |
| **Focused Handlers** | Each handler should do one thing | `UpdateHealthUI()` |
| **Error Handling** | Handle errors gracefully | Try-catch blocks |
| **Clean Up Handlers** | Dispose handlers when done | `OnDestroy()` cleanup |
| **Call AggregateReady** | Signal readiness after registration | `EventBus.AggregateReady(playerId)` |
| **Monitor EventBus** | Check queue and buffered event counts | `GetQueueCount()`, `GetTotalBufferedEventCount()` |