# Unity EventBus

A high-performance event bus system for Unity with automatic thread management.

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
    void Start()
    {
        EventBus.RegisterHandler<PlayerDiedEvent>(OnPlayerDied, requiresMainThread: true);
    }

    private void OnPlayerDied(PlayerDiedEvent evt)
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
- `RegisterHandler<T>(Action<T> handler, bool requiresMainThread = true)` - Register handler
- `Instance.Send<T>(T eventData)` - Send event asynchronously  
- `Instance.SendAndWait<T>(T eventData)` - Send event synchronously
- `Instance.Shutdown()` - Cleanup resources

### Dispatchers
- **UnityDispatcher**: Main thread execution
- **ThreadPoolDispatcher**: Background thread execution  
- **ImmediateDispatcher**: Immediate execution

## License

MIT License