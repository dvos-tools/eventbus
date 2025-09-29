# Unity EventBus

A high-performance event bus system for Unity 6 with automatic thread management, flexible dispatcher support, and aggregate-based event routing.

## Installation

Add to your `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.dvos-tools.bus": "file:../path/to/unitybus"
  }
}
```

## Quick Start

```csharp
using com.DvosTools.bus;

// Define a regular event
public class PlayerDiedEvent
{
    public string PlayerName { get; set; }
    public int Score { get; set; }
}

// Define a routable event (with aggregate ID)
public class PlayerHealthChangedEvent : IRoutableEvent
{
    public Guid AggregateId { get; set; }
    public string PlayerName { get; set; }
    public int NewHealth { get; set; }
}

// Subscribe to events
public class GameManager : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeStaticHandlers()
    {
        // Regular handler (receives all events of this type)
        EventBus.RegisterHandler<PlayerDiedEvent>(HandlePlayerDiedEvent);
        
        // Routed handler (only receives events for specific player)
        var playerId = Guid.NewGuid();
        EventBus.RegisterHandler<PlayerHealthChangedEvent>(HandleHealthChanged, playerId);
    }

    private static void HandlePlayerDiedEvent(PlayerDiedEvent evt)
    {
        Debug.Log($"Player {evt.PlayerName} died with score {evt.Score}");
    }
    
    private static void HandleHealthChanged(PlayerHealthChangedEvent evt)
    {
        Debug.Log($"Player {evt.PlayerName} health: {evt.NewHealth}");
    }
}

// Publish events
public class PlayerController : MonoBehaviour
{
    private Guid playerId = Guid.NewGuid();
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Regular event (all handlers receive this)
            EventBus.Instance.Send(new PlayerDiedEvent 
            { 
                PlayerName = "Player1", 
                Score = 1000 
            });
            
            // Routed event (only handlers with matching aggregate ID receive this)
            EventBus.Instance.Send(new PlayerHealthChangedEvent 
            { 
                AggregateId = playerId,
                PlayerName = "Player1", 
                NewHealth = 80 
            });
        }
    }
}
```

## Routing

Events can be routed to specific handlers using aggregate IDs:

- **Regular Events**: All handlers of the event type receive the event
- **Routed Events**: Only handlers registered with matching aggregate ID receive the event
- **Mixed Usage**: You can have both regular and routed handlers for the same event type

This allows multiple instances of the same handler class to receive events for different entities (e.g., different players, different game objects).

## API

### EventBus
- `RegisterHandler<T>(Action<T> handler, Guid aggregateId = default, IDispatcher dispatcher = null)` - Register handler with optional routing and custom dispatcher
- `RegisterStaticHandler<T>(Action<T> handler, IDispatcher dispatcher = null)` - Register regular handler (backwards compatibility, marked obsolete)
- `Instance.Send<T>(T eventData)` - Send event asynchronously  
- `Instance.SendAndWait<T>(T eventData)` - Send event synchronously
- `Instance.Shutdown()` - Cleanup resources
- `EnableLogging` - Static property to enable/disable debug logging (default: false)

**Note:** `RegisterStaticHandler` is provided for backwards compatibility but is marked as obsolete. It internally calls `RegisterHandler` with `Guid.Empty`. For new code, use `RegisterHandler` instead.

### Dispatchers
- **UnityDispatcher**: Main thread execution
- **ThreadPoolDispatcher**: Background thread execution  
- **ImmediateDispatcher**: Immediate execution

## Development Workflow

```bash
# Development
feat: add new dispatcher → Merge to main → Version: 1.0.0 → 1.1.0
fix: resolve bug → Merge to main → Version: 1.1.0 → 1.1.1

# When ready to release:
# GitHub Actions → Manual Release → Enter "1.1.1" → Creates release
```

## License

MIT License