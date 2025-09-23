# Unity EventBus

A high-performance event bus system for Unity 6 with automatic thread management and flexible dispatcher support.

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

// Define an event
public class PlayerDiedEvent
{
    public string PlayerName { get; set; }
    public int Score { get; set; }
}

// Subscribe to events
public class GameManager : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeStaticHandlers()
    {
        EventBus.RegisterStaticHandler<PlayerDiedEvent>(HandlePlayerDiedEvent);
    }

    private static void HandlePlayerDiedEvent(PlayerDiedEvent evt)
    {
        Debug.Log($"Player {evt.PlayerName} died with score {evt.Score}");
    }
}

// Publish events
public class PlayerController : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            EventBus.Instance.Send(new PlayerDiedEvent 
            { 
                PlayerName = "Player1", 
                Score = 1000 
            });
        }
    }
}
```

## API

### EventBus
- `RegisterHandler<T>(Action<T> handler, IDispatcher dispatcher = null)` - Register handler with optional custom dispatcher
- `Instance.Send<T>(T eventData)` - Send event asynchronously  
- `Instance.SendAndWait<T>(T eventData)` - Send event synchronously
- `Instance.Shutdown()` - Cleanup resources

### Dispatchers
- **UnityDispatcher**: Main thread execution
- **ThreadPoolDispatcher**: Background thread execution  
- **ImmediateDispatcher**: Immediate execution

## License

MIT License