# Unity EventBus

A high-performance event bus system for Unity with automatic thread management and aggregate-based event routing.

## What is Unity EventBus?

Unity EventBus is a powerful event system that allows you to:
- **Decouple your code** - Objects communicate through events instead of direct references
- **Route events** - Send events to specific aggregates (like players, enemies, etc.)
- **Buffer events** - Store events until aggregates are ready to process them
- **Thread safely** - Automatic thread management with Unity main thread execution

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

// 1. Define an event
public class PlayerDiedEvent
{
    public string PlayerName { get; set; }
    public int Score { get; set; }
}

// 2. Subscribe to events
void Start()
{
    EventBus.RegisterUnityHandler<PlayerDiedEvent>(OnPlayerDied);
}

// 3. Handle events
void OnPlayerDied(PlayerDiedEvent evt)
{
    Debug.Log($"Player {evt.PlayerName} died with score {evt.Score}");
}

// 4. Send events
EventBus.Send(new PlayerDiedEvent { PlayerName = "Player1", Score = 1000 });
```

## Documentation

📚 **Complete documentation is available in multiple formats:**

### Online (GitHub Wiki)
- **[Wiki Home](https://github.com/dvos-tools/eventbus/wiki)** - Start here for an overview
- **[Quick Start Guide](https://github.com/dvos-tools/eventbus/wiki/Quick-Start-Guide)** - Get up and running quickly
- **[Core Concepts](https://github.com/dvos-tools/eventbus/wiki/Core-Concepts)** - Learn the fundamentals
- **[Your First Event System](https://github.com/dvos-tools/eventbus/wiki/Your-First-Event-System)** - Complete example
- **[Threading and Dispatchers](https://github.com/dvos-tools/eventbus/wiki/Threading-and-Dispatchers)** - Threading model
- **[Event Routing](https://github.com/dvos-tools/eventbus/wiki/Event-Routing)** - Targeted event delivery
- **[Event Buffering](https://github.com/dvos-tools/eventbus/wiki/Event-Buffering)** - Event storage and processing
- **[Best Practices](https://github.com/dvos-tools/eventbus/wiki/Best-Practices)** - Professional patterns
- **[API Reference](https://github.com/dvos-tools/eventbus/wiki/API-Reference)** - Complete method reference

## License

MIT License