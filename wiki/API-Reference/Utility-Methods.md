# Utility Methods

Methods for monitoring, debugging, and system management.

## Queue Management

### `EventBus.GetQueueCount()`

**What it does:** Returns the number of events currently in the processing queue.

**When to use:** For monitoring system load and health.

**Returns:** Number of events in the queue

**Example:**
```csharp
// Monitor queue size
int queueSize = EventBus.GetQueueCount();
if (queueSize > 100)
{
    Debug.LogWarning($"Event queue is getting large: {queueSize} events");
}
```

**Why use this:** Use this to monitor the health of your event system. A large queue might indicate that handlers are taking too long to execute or that events are being sent too frequently.

### `EventBus.HasQueuedEvents()`

**What it does:** Checks if there are any events currently in the processing queue.

**When to use:** For quick status checks.

**Returns:** True if there are events in the queue, false otherwise

**Example:**
```csharp
// Wait for all events to process
while (EventBus.HasQueuedEvents())
{
    await Task.Delay(10);
}
Debug.Log("All events processed");
```

**Why use this:** Use this for quick checks to see if there are any events waiting to be processed. This is useful for debugging or for determining if the system is ready to proceed.

## Handler Monitoring

### `EventBus.GetHandlerCount<T>()`

**What it does:** Returns the number of registered handlers for a specific event type.

**When to use:** For debugging, monitoring, or ensuring proper handler registration.

**Parameters:**
- `T`: The type of event to check

**Returns:** Number of registered handlers

**Example:**
```csharp
// Check if handlers are registered
int handlerCount = EventBus.GetHandlerCount<PlayerDiedEvent>();
if (handlerCount == 0)
{
    Debug.LogWarning("No handlers registered for PlayerDiedEvent");
}
```

**Why use this:** Use this to verify that handlers are properly registered for specific event types. This is useful for debugging or for ensuring that all necessary handlers are in place.

### `EventBus.HasHandlers<T>()`

**What it does:** Checks if there are any handlers registered for a specific event type.

**When to use:** For quick handler existence checks.

**Parameters:**
- `T`: The type of event to check

**Returns:** True if handlers are registered, false otherwise

**Example:**
```csharp
// Only send event if handlers exist
if (EventBus.HasHandlers<PlayerDiedEvent>())
{
    EventBus.Send(new PlayerDiedEvent { PlayerName = "Player1" });
}
```

**Why use this:** Use this for quick checks to see if there are any handlers registered for a specific event type. This is useful for conditional event sending or for debugging.

## System Management

### `EventBus.Shutdown()`

**What it does:** Shuts down the event bus, cancelling background processing.

**When to use:** When shutting down the application or switching scenes.

**Example:**
```csharp
// Shutdown when application quits
void OnApplicationQuit()
{
    EventBus.Shutdown();
}

// Shutdown when changing scenes
void OnSceneChange()
{
    EventBus.Shutdown();
}
```

**Why use this:** Use this to properly shut down the event bus when you're done with it. This cancels background processing and cleans up resources.

### `EventBus.Cleanup()`

**What it does:** Cleans up all resources and resets the event bus state completely.

**When to use:** For complete system reset (recreates singletons on next access).

**Example:**
```csharp
// Complete reset between test runs
[SetUp]
public void Setup()
{
    EventBus.Cleanup();
}

// Complete reset when changing game modes
void OnGameModeChange()
{
    EventBus.Cleanup();
}
```

**Why use this:** Use this for complete system reset. This clears all handlers and events, and recreates singletons on next access. This is useful for testing or for completely resetting the event system.

## Logging and Debugging

### `EventBusLogger.EnableLogging`

**What it does:** Enables or disables event bus logging.

**When to use:** For debugging event flow and handler execution.

**Example:**
```csharp
// Enable logging for debugging
EventBusLogger.EnableLogging = true;

// Your event code...

// Disable logging for production
EventBusLogger.EnableLogging = false;
```

**Why use this:** Use this to enable detailed logging of event flow and handler execution. This is useful for debugging but should be disabled in production.

## System Monitoring

### System Health Check
```csharp
public class EventBusHealthMonitor : MonoBehaviour
{
    void Update()
    {
        // Monitor system health
        int queueSize = EventBus.GetQueueCount();
        int totalBuffered = EventBus.GetTotalBufferedEventCount();
        
        if (queueSize > 1000)
        {
            Debug.LogWarning($"High queue size: {queueSize}");
        }
        
        if (totalBuffered > 5000)
        {
            Debug.LogWarning($"High buffered events: {totalBuffered}");
        }
    }
}
```

### Handler Registration Monitoring
```csharp
public class HandlerMonitor : MonoBehaviour
{
    void Start()
    {
        // Monitor handler registration
        InvokeRepeating(nameof(CheckHandlers), 1f, 5f);
    }
    
    void CheckHandlers()
    {
        var eventTypes = new[] 
        { 
            typeof(PlayerDiedEvent), 
            typeof(PlayerHealthChangedEvent),
            typeof(GameStateChangedEvent)
        };
        
        foreach (var eventType in eventTypes)
        {
            var method = typeof(EventBus).GetMethod("GetHandlerCount");
            var genericMethod = method.MakeGenericMethod(eventType);
            int count = (int)genericMethod.Invoke(null, null);
            
            Debug.Log($"{eventType.Name}: {count} handlers");
        }
    }
}
```

## Common Patterns

### 1. System Monitoring
```csharp
public class SystemMonitor : MonoBehaviour
{
    void Update()
    {
        // Monitor queue size
        int queueSize = EventBus.GetQueueCount();
        if (queueSize > 100)
        {
            Debug.LogWarning($"High event queue size: {queueSize}");
        }
        
        // Monitor buffered events
        int totalBuffered = EventBus.GetTotalBufferedEventCount();
        if (totalBuffered > 1000)
        {
            Debug.LogWarning($"High buffered event count: {totalBuffered}");
        }
    }
}
```

### 2. Debugging Event Flow
```csharp
public class EventDebugger : MonoBehaviour
{
    void Start()
    {
        // Enable logging
        EventBusLogger.EnableLogging = true;
        
        // Monitor all events
        EventBus.RegisterUnityHandler<object>(OnAnyEvent);
    }
    
    void OnAnyEvent(object evt)
    {
        Debug.Log($"Event sent: {evt.GetType().Name}");
    }
}
```

### 3. System Cleanup
```csharp
public class SystemManager : MonoBehaviour
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
    
    void OnApplicationQuit()
    {
        // Shutdown when quitting
        EventBus.Shutdown();
    }
}
```

## Best Practices

### 1. Monitor System Health
```csharp
// Good: Monitor system health
void Update()
{
    int queueSize = EventBus.GetQueueCount();
    if (queueSize > 1000)
    {
        Debug.LogWarning($"High queue size: {queueSize}");
    }
}

// Bad: No monitoring
void Update()
{
    // No monitoring - problems go unnoticed
}
```

### 2. Use Appropriate Cleanup Methods
```csharp
// Good: Use specific cleanup methods
EventBus.DisposeHandlers<PlayerEvent>(); // Only player events
EventBus.ClearEventsForAggregate(playerId); // Only this player

// Bad: Over-cleanup
EventBus.Cleanup(); // Clears everything unnecessarily
```

### 3. Enable Logging During Development
```csharp
// Good: Enable logging for debugging
#if UNITY_EDITOR
EventBusLogger.EnableLogging = true;
#endif

// Bad: Always enabled
EventBusLogger.EnableLogging = true; // Logging impact in production
```

## Common Mistakes

### ❌ Don't: Leave logging enabled in production
```csharp
// WRONG - Unnecessary overhead
EventBusLogger.EnableLogging = true; // Always enabled
```

### ❌ Don't: Ignore system health
```csharp
// WRONG - No monitoring
void Update()
{
    // No monitoring - problems go unnoticed
}
```

### ✅ Do: Monitor and clean up appropriately
```csharp
// CORRECT - Proper monitoring and cleanup
void Update()
{
    // Monitor system health
    int queueSize = EventBus.GetQueueCount();
    if (queueSize > 1000)
    {
        Debug.LogWarning($"High queue size: {queueSize}");
    }
}

void OnDestroy()
{
    // Clean up when done
    EventBus.DisposeHandlers<PlayerEvent>();
}
```

## System Considerations

1. **Monitor queue sizes** - Use `GetQueueCount()` to monitor system health
2. **Enable logging only when needed** - Don't leave it enabled in production
3. **Use appropriate cleanup methods** - Choose the right method for your use case
4. **Profile utility operations** - Monitor the execution time of these operations
5. **Test edge cases** - Ensure proper behavior in all scenarios