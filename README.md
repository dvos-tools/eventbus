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

📚 **Complete documentation is available in the `wiki/` folder:**

- **[01-Home](wiki/01-Home.md)** - Start here for an overview
- **[02-Quick-Start-Guide](wiki/02-Quick-Start-Guide.md)** - Get up and running quickly
- **[03-Core-Concepts](wiki/03-Core-Concepts.md)** - Learn the fundamentals
- **[04-Your-First-Event-System](wiki/04-Your-First-Event-System.md)** - Complete example
- **[05-Threading-and-Dispatchers](wiki/05-Threading-and-Dispatchers.md)** - Threading model
- **[06-Event-Routing](wiki/06-Event-Routing.md)** - Targeted event delivery
- **[07-Event-Buffering](wiki/07-Event-Buffering.md)** - Event storage and processing
- **[08-Best-Practices](wiki/08-Best-Practices.md)** - Professional patterns
- **[09-Common-Patterns](wiki/09-Common-Patterns.md)** - Real-world examples
- **[10-Troubleshooting](wiki/10-Troubleshooting.md)** - Common problems and solutions
- **[API-Reference](wiki/API-Reference/)** - Complete method reference

## License

MIT License