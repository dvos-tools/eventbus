# Unity EventBus Documentation

Welcome to the Unity EventBus documentation! This event bus system provides automatic thread management and aggregate-based event routing for Unity applications.

## 📖 How to Read This Documentation

This documentation is structured as a learning path - read the pages in order for the best experience:

### **Part 1: Getting Started**
1. **[02-Quick Start Guide](./02-Quick-Start-Guide)** - Get up and running in 5 minutes
2. **[03-Core Concepts](03-Core-Concepts)** - Understand events, handlers, and the basics
3. **[04-Your First Event System](04-Your-First-Event-System)** - Build a complete example

### **Part 2: Essential Features**
4. **[05-Threading and Dispatchers](05-Threading-and-Dispatchers)** - Learn about UnityDispatcher (the only supported dispatcher)
5. **[06-Event Routing](06-Event-Routing)** - Master targeted event delivery
6. **[07-Event Buffering](07-Event-Buffering)** - Handle events when objects aren't ready yet

### **Part 3: Advanced Topics**
7. **[08-Best Practices](08-Best-Practices)** - Professional patterns and recommendations
8. **[09-Common Patterns](09-Common-Patterns)** - Real-world examples
9. **[10-Common Patterns](10-Common-Patterns)** - Real-world usage examples

### **Part 4: Reference**
10. **[API-Reference](API-Reference)** - Complete method documentation
11. **[11-Troubleshooting](11-Troubleshooting)** - Solutions to common problems

## 🚀 Quick Start

If you're in a hurry, here's the absolute minimum to get started:

```csharp
using com.DvosTools.bus;

// 1. Define an event
public class PlayerDiedEvent
{
    public string PlayerName { get; set; }
    public int Score { get; set; }
}

// 2. Subscribe to it
EventBus.RegisterUnityHandler<PlayerDiedEvent>(OnPlayerDied);

// 3. Handle the event
void OnPlayerDied(PlayerDiedEvent evt)
{
    Debug.Log($"Player {evt.PlayerName} died with score {evt.Score}");
}

// 4. Send the event
EventBus.Send(new PlayerDiedEvent { PlayerName = "Player1", Score = 1000 });
```

## 🎯 What You'll Learn

By the end of this documentation, you'll know how to:

- ✅ Set up event-driven architecture in Unity
- ✅ Handle threading safely with UnityDispatcher
- ✅ Route events to specific game objects and systems
- ✅ Buffer events until objects are ready to process them
- ✅ Handle high-frequency event processing
- ✅ Debug and troubleshoot event systems
- ✅ Apply professional patterns and best practices

## 📋 Prerequisites

- Basic C# knowledge
- Unity 2020.3 or later
- Understanding of Unity's component system

## 🔧 Installation

Add to your `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.dvos-tools.bus": "file:../path/to/unitybus"
  }
}
```

## 🤔 Why Use Unity EventBus?

- **Loose Coupling**: Components communicate without direct references
- **Thread Safety**: Built-in UnityDispatcher prevents Unity threading issues
- **Efficiency**: Designed for high-frequency event processing
- **Unity Integration**: Designed specifically for Unity's architecture
- **Simple API**: Easy to use with comprehensive documentation

---

**Ready to start?** Begin with the [02-Quick Start Guide](02-Quick-Start-Guide) to get your first event system running in minutes!