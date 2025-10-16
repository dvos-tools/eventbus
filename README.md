# Unity EventBus

A high-performance event bus system for Unity with automatic thread management and aggregate-based event routing.

## Installation

Add to your `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.dvos-tools.bus": "file:../path/to/unitybus"
  }
}
```

## Usage

```csharp
using com.DvosTools.bus;

// Define events
public class PlayerDiedEvent
{
    public string PlayerName { get; set; }
    public int Score { get; set; }
}

public class PlayerHealthChangedEvent : IRoutableEvent
{
    public Guid AggregateId { get; set; }
    public string PlayerName { get; set; }
    public int NewHealth { get; set; }
}

// Subscribe to events
EventBus.RegisterHandler<PlayerDiedEvent>(OnPlayerDied);
EventBus.RegisterHandler<PlayerHealthChangedEvent>(OnHealthChanged, playerId);

// Event handlers
void OnPlayerDied(PlayerDiedEvent evt)
{
    Debug.Log($"Player {evt.PlayerName} died with score {evt.Score}");
}

void OnHealthChanged(PlayerHealthChangedEvent evt)
{
    Debug.Log($"Player {evt.PlayerName} health: {evt.NewHealth}");
}

// Send events
EventBus.Send(new PlayerDiedEvent { PlayerName = "Player1", Score = 1000 });
EventBus.Send(new PlayerHealthChangedEvent { AggregateId = playerId, NewHealth = 80 });

// Process buffered events when ready
EventBus.AggregateReady(playerId);
```

## API

- `EventBus.RegisterHandler<T>(Action<T> handler, Guid aggregateId = default, IDispatcher dispatcher = null)`
- `EventBus.Instance.Send<T>(T eventData)` - Async
- `EventBus.Instance.SendAndWait<T>(T eventData)` - Sync
- `EventBusLogger.EnableLogging` - Enable/disable logging

## Dispatchers

| Dispatcher               | Execution | FIFO Order  | Use Case              |
|--------------------------|-----------|-------------|-----------------------|
| **UnityDispatcher**      | Async     | ✅ Ordered   | Unity main thread     |
| **ThreadPoolDispatcher** | Async     | ❌ Unordered | Background processing |
| **ImmediateDispatcher**  | Sync      | ✅ Ordered   | Immediate execution   |

## License

MIT License