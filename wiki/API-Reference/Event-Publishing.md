# Event Publishing

Methods for sending events to registered handlers.

## `EventBus.Send<T>(T eventData)`

**What it does:** Publishes an event asynchronously to all registered subscribers.

**When to use:** For fire-and-forget event publishing where you don't need to wait for handlers to complete.

**Parameters:**
- `eventData` (T): The event data to send

**Example:**
```csharp
// Send a player death event
EventBus.Send(new PlayerDiedEvent 
{ 
    PlayerName = "Player1", 
    Score = 1000 
});

// Send a routed event to a specific player
EventBus.Send(new PlayerHealthChangedEvent 
{ 
    AggregateId = playerId, 
    NewHealth = 80 
});
```

**Why use this:** This is the most common method for event publishing. It's non-blocking and allows your code to continue immediately while handlers process the event in the background.

**Speed:** Very fast, returns immediately. Handlers are processed asynchronously.

## `EventBus.SendAndWait<T>(T eventData)`

**What it does:** Publishes an event and blocks until all handlers complete processing.

**When to use:** For critical events where you need guaranteed processing order or when the next operation depends on the event being fully handled.

**Parameters:**
- `eventData` (T): The event data to send

**Example:**
```csharp
// Send a critical save event and wait for completion
EventBus.SendAndWait(new SaveGameEvent 
{ 
    SaveSlot = 1, 
    PlayerData = currentPlayerData 
});

// Now it's safe to continue - all save handlers have completed
Debug.Log("Game saved successfully!");
```

**Why use this:** Use this when you need to ensure that all side effects of an event have been processed before continuing. This is especially important for critical operations like saving, loading, or state transitions.

**Speed:** Slower than `Send()` as it blocks the calling thread. Use sparingly.

## Threading Considerations

### Send() - Asynchronous
```csharp
// This returns immediately
EventBus.Send(new PlayerEvent { Data = "test" });
Debug.Log("This runs immediately, before handlers complete");
```

### SendAndWait() - Synchronous
```csharp
// This blocks until all handlers complete
EventBus.SendAndWait(new PlayerEvent { Data = "test" });
Debug.Log("This runs after all handlers complete");
```

## Event Routing

Both methods support routed events (events implementing `IRoutableEvent`):

```csharp
// Routed event - only handlers for this aggregate receive it
EventBus.Send(new PlayerHealthChangedEvent 
{ 
    AggregateId = playerId,  // This makes it routed
    NewHealth = 80 
});

// Non-routed event - all handlers receive it
EventBus.Send(new GameStartedEvent 
{ 
    LevelName = "Level 1" 
});
```

## Error Handling

Events are processed in a try-catch block, so handler errors won't crash your application:

```csharp
// If a handler throws an exception, it's logged but doesn't crash the system
EventBus.Send(new PlayerEvent { Data = "test" });
// Your code continues even if handlers fail
```

## Best Practices

1. **Use `Send()` for most cases** - It's faster and non-blocking
2. **Use `SendAndWait()` sparingly** - Only when you need guaranteed completion
3. **Handle errors gracefully** - Don't let handler errors crash your system
4. **Consider event ordering** - Use `SendAndWait()` when order matters
5. **Profile execution** - Monitor event frequency and handler execution time

## Common Patterns

### Fire-and-Forget
```csharp
// Good for UI updates, sound effects, etc.
EventBus.Send(new PlayerMovedEvent { Position = transform.position });
```

### Critical Operations
```csharp
// Good for save/load, state transitions, etc.
EventBus.SendAndWait(new SaveGameEvent { Slot = 1 });
```

### Batch Processing
```csharp
// Send multiple events
foreach (var action in actions)
{
    EventBus.Send(new ActionEvent { Action = action });
}
```

## Optimization Tips

1. **Minimize event frequency** - Don't send events every frame
2. **Use appropriate dispatchers** - Choose the right thread for your handlers
3. **Keep handlers lightweight** - Avoid heavy computations in handlers
4. **Monitor queue sizes** - Use `GetQueueCount()` to monitor system health
5. **Profile your handlers** - Use stopwatch to measure handler execution time