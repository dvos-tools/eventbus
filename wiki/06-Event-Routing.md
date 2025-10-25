# Event Routing

Event routing allows you to send events to specific game objects or systems, enabling precise event targeting and better system organization.

## What is Event Routing?

Event routing lets you send events to specific "aggregates" (game objects or logical entities) rather than broadcasting to all handlers. This is like having a postal system where you can address events to specific recipients.

## Basic vs Routed Events

### Basic Events (Broadcast)
```csharp
// Basic event - goes to ALL handlers
public class GameStartedEvent
{
    public string LevelName { get; set; }
}

// All handlers receive this event
EventBus.Send(new GameStartedEvent { LevelName = "Level 1" });
```

### Routed Events (Targeted)
```csharp
// Routed event - goes to SPECIFIC handlers
public class PlayerHealthChangedEvent : IRoutableEvent
{
    public Guid AggregateId { get; set; }  // This makes it routable
    public int NewHealth { get; set; }
    public int MaxHealth { get; set; }
}

// Only handlers registered for this specific player receive the event
EventBus.Send(new PlayerHealthChangedEvent 
{ 
    AggregateId = playerId, 
    NewHealth = 80, 
    MaxHealth = 100 
});
```

## The IRoutableEvent Interface

To make an event routable, implement the `IRoutableEvent` interface:

```csharp
public interface IRoutableEvent
{
    Guid AggregateId { get; }
}
```

**Example:**
```csharp
public class PlayerMovedEvent : IRoutableEvent
{
    public Guid AggregateId { get; set; }
    public Vector3 OldPosition { get; set; }
    public Vector3 NewPosition { get; set; }
    public float MovementSpeed { get; set; }
}
```

## How Event Routing Works

### 1. Event Publishing
When you send a routed event, the EventBus checks if there are handlers registered for that specific aggregate ID:

```csharp
// Send event to specific player
var playerId = Guid.NewGuid();
EventBus.Send(new PlayerHealthChangedEvent 
{ 
    AggregateId = playerId,
    NewHealth = 80,
    MaxHealth = 100
});
```

### 2. Handler Registration
Handlers can be registered for specific aggregates or globally:

```csharp
// Register handler for specific player
EventBus.RegisterUnityHandler<PlayerHealthChangedEvent>(
    OnPlayerHealthChanged, 
    playerId
);

// Register global handler (receives all events of this type)
EventBus.RegisterUnityHandler<PlayerHealthChangedEvent>(
    OnGlobalHealthChanged, 
    Guid.Empty
);
```

### 3. Event Processing
- **Routed events**: Only handlers registered for the specific aggregate ID receive the event
- **Non-routed events**: All handlers receive the event regardless of aggregate ID
- **Global handlers**: Always receive events (registered with `Guid.Empty`)

## Real-World Example: Multiplayer Game

Let's build a multiplayer game system using event routing:

### Step 1: Define Player Events
```csharp
public class PlayerJoinedEvent : IRoutableEvent
{
    public Guid AggregateId { get; set; }
    public string PlayerName { get; set; }
    public Vector3 SpawnPosition { get; set; }
}

public class PlayerMovedEvent : IRoutableEvent
{
    public Guid AggregateId { get; set; }
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
}

public class PlayerAttackedEvent : IRoutableEvent
{
    public Guid AggregateId { get; set; }
    public Guid TargetPlayerId { get; set; }
    public int Damage { get; set; }
}
```

### Step 2: Create Player Controller
```csharp
public class PlayerController : MonoBehaviour
{
    public Guid PlayerId { get; private set; }
    public string PlayerName { get; set; }
    
    void Start()
    {
        PlayerId = Guid.NewGuid();
        
        // Register handlers for this specific player
        EventBus.RegisterUnityHandler<PlayerMovedEvent>(OnPlayerMoved, PlayerId);
        EventBus.RegisterUnityHandler<PlayerAttackedEvent>(OnPlayerAttacked, PlayerId);
        
        // Send player joined event
        EventBus.Send(new PlayerJoinedEvent
        {
            AggregateId = PlayerId,
            PlayerName = PlayerName,
            SpawnPosition = transform.position
        });
    }
    
    void OnPlayerMoved(PlayerMovedEvent evt)
    {
        // Only this player receives their own movement events
        transform.position = evt.Position;
        transform.rotation = evt.Rotation;
    }
    
    void OnPlayerAttacked(PlayerAttackedEvent evt)
    {
        // This player was attacked
        if (evt.TargetPlayerId == PlayerId)
        {
            TakeDamage(evt.Damage);
        }
    }
    
    void TakeDamage(int damage)
    {
        // Handle damage logic
        Debug.Log($"{PlayerName} took {damage} damage!");
    }
}
```

### Step 3: Create Game Manager
```csharp
public class GameManager : MonoBehaviour
{
    private Dictionary<Guid, PlayerController> players = new();
    
    void Start()
    {
        // Register global handlers
        EventBus.RegisterUnityHandler<PlayerJoinedEvent>(OnPlayerJoined);
        EventBus.RegisterUnityHandler<PlayerMovedEvent>(OnPlayerMoved);
        EventBus.RegisterUnityHandler<PlayerAttackedEvent>(OnPlayerAttacked);
    }
    
    void OnPlayerJoined(PlayerJoinedEvent evt)
    {
        // Track all players
        var player = FindObjectOfType<PlayerController>();
        if (player != null && player.PlayerId == evt.AggregateId)
        {
            players[evt.AggregateId] = player;
            Debug.Log($"Player {evt.PlayerName} joined the game!");
        }
    }
    
    void OnPlayerMoved(PlayerMovedEvent evt)
    {
        // Update other players about this player's movement
        foreach (var kvp in players)
        {
            if (kvp.Key != evt.AggregateId) // Don't send to the player who moved
            {
                // Send movement update to other players
                // This could trigger network updates, AI responses, etc.
            }
        }
    }
    
    void OnPlayerAttacked(PlayerAttackedEvent evt)
    {
        // Handle attack logic
        if (players.TryGetValue(evt.TargetPlayerId, out var targetPlayer))
        {
            // The target player will handle the damage via their own handler
            Debug.Log($"Player {evt.AggregateId} attacked {evt.TargetPlayerId}");
        }
    }
}
```

## Event Buffering

One of the most powerful features of Unity EventBus is **event buffering**. When you send a routed event to an aggregate that doesn't have any handlers yet, the event gets buffered until the aggregate is ready.

### How Buffering Works

1. **Event sent to non-ready aggregate**: Event is stored in a buffer
2. **Aggregate becomes ready**: Call `EventBus.AggregateReady(aggregateId)`
3. **Buffered events processed**: All buffered events are processed in order

**Example:**
```csharp
// Player object not ready yet - events get buffered
EventBus.Send(new PlayerHealthChangedEvent 
{ 
    AggregateId = playerId,
    NewHealth = 100 
});

EventBus.Send(new PlayerMovedEvent 
{ 
    AggregateId = playerId,
    Position = new Vector3(10, 0, 5)
});

// Later, when player is ready...
EventBus.AggregateReady(playerId);
// Both events are now processed in order!
```

### Using Buffering in Practice

```csharp
public class PlayerController : MonoBehaviour
{
    public Guid PlayerId { get; private set; }
    
    void Start()
    {
        PlayerId = Guid.NewGuid();
        
        // Register handlers
        EventBus.RegisterUnityHandler<PlayerHealthChangedEvent>(OnHealthChanged, PlayerId);
        EventBus.RegisterUnityHandler<PlayerMovedEvent>(OnMoved, PlayerId);
        
        // Signal that this player is ready to process buffered events
        EventBus.AggregateReady(PlayerId);
    }
    
    void OnHealthChanged(PlayerHealthChangedEvent evt)
    {
        // Handle health changes
        UpdateHealthUI(evt.NewHealth);
    }
    
    void OnMoved(PlayerMovedEvent evt)
    {
        // Handle movement
        transform.position = evt.Position;
    }
}
```

## Aggregate Management

### `EventBus.AggregateReady(Guid aggregateId)`
Signals that an aggregate is ready to process its buffered events.

### `EventBus.ClearEventsForAggregate(Guid aggregateId)`
Clears all queued and buffered events for a specific aggregate.

### `EventBus.ResetAggregate(Guid aggregateId)`
Removes all handlers and clears all events for an aggregate.

## Common Routing Patterns

### 1. Player-Specific Events
```csharp
// Events that only affect one player
public class PlayerInventoryChangedEvent : IRoutableEvent
{
    public Guid AggregateId { get; set; }
    public List<Item> Items { get; set; }
}
```

### 2. System-Specific Events
```csharp
// Events that only affect one system
public class InventorySystemEvent : IRoutableEvent
{
    public Guid AggregateId { get; set; }
    public string Action { get; set; }
    public object Data { get; set; }
}
```

### 3. Mixed Events
```csharp
// Events that can be both routed and global
public class PlayerDiedEvent : IRoutableEvent
{
    public Guid AggregateId { get; set; }
    public string PlayerName { get; set; }
    public int FinalScore { get; set; }
}

// Register for specific player
EventBus.RegisterUnityHandler<PlayerDiedEvent>(OnPlayerDied, playerId);

// Register globally
EventBus.RegisterUnityHandler<PlayerDiedEvent>(OnGlobalPlayerDied, Guid.Empty);
```

## Best Practices

1. **Use meaningful aggregate IDs** - Generate them consistently
2. **Signal readiness at the right time** - After handlers are registered
3. **Clean up when done** - Reset aggregates when objects are destroyed
4. **Choose appropriate event types** - Use routed events for targeted communication
5. **Handle buffering properly** - Call `AggregateReady()` when appropriate

## Next Steps

Now that you understand event routing, let's learn about event buffering in detail:

**➡️ Continue to [07-Event Buffering](07-Event-Buffering)**

---

## Quick Reference

| Concept | Description | Example |
|---------|-------------|---------|
| **Basic Event** | Broadcasts to all handlers | `GameStartedEvent` |
| **Routed Event** | Targets specific aggregates | `PlayerHealthChangedEvent : IRoutableEvent` |
| **Aggregate ID** | Unique identifier for targeting | `Guid.NewGuid()` |
| **Global Handler** | Receives all events of a type | `RegisterHandler(handler, Guid.Empty)` |
| **Routed Handler** | Receives events for specific aggregate | `RegisterHandler(handler, aggregateId)` |