# Unity EventBus

An event bus system for Unity with automatic thread management and aggregate-based event routing.

## What is Unity EventBus?

Unity EventBus is a powerful event system that allows you to:
- **Decouple your code** - Objects communicate through events instead of direct references
- **Route events** - Send events to specific aggregates (like players, enemies, etc.)
- **Buffer events** - Store events until aggregates are ready to process them

---

## Installation

Add to your `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.dvos-tools.bus": "file:../path/to/unitybus"
  }
}
```

---

## Documentation
📚 **Complete documentation is available in multiple formats:**
- **[Wiki Home](https://github.com/dvos-tools/eventbus/wiki)** - Start here for an overview

---

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
---

## License

MIT License