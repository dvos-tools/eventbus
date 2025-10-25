# Quick Start Guide

Get Unity EventBus running in your project in just 5 minutes! This guide will walk you through creating your first event system.

## Step 1: Installation

Add Unity EventBus to your project by adding this to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.dvos-tools.bus": "file:../path/to/unitybus"
  }
}
```

## Step 2: Create Your First Event

Create a simple event class:

```csharp
using com.DvosTools.bus;

public class PlayerHealthChangedEvent
{
    public string PlayerName { get; set; }
    public int NewHealth { get; set; }
    public int MaxHealth { get; set; }
}
```

## Step 3: Create a Handler

Create a MonoBehaviour that will handle the event:

```csharp
using UnityEngine;
using com.DvosTools.bus;

public class HealthDisplay : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Text healthText;
    
    void Start()
    {
        // Subscribe to health change events
        EventBus.RegisterUnityHandler<PlayerHealthChangedEvent>(OnHealthChanged);
    }
    
    void OnHealthChanged(PlayerHealthChangedEvent evt)
    {
        // Update the UI
        healthText.text = $"Health: {evt.NewHealth}/{evt.MaxHealth}";
    }
}
```

## Step 4: Send Events

Create another script that sends events:

```csharp
using UnityEngine;
using com.DvosTools.bus;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private int currentHealth = 100;
    [SerializeField] private int maxHealth = 100;
    
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        
        // Send the health change event
        EventBus.Send(new PlayerHealthChangedEvent
        {
            PlayerName = gameObject.name,
            NewHealth = currentHealth,
            MaxHealth = maxHealth
        });
    }
}
```

## Step 5: Test It Out

1. Create a GameObject with the `PlayerController` script
2. Create a UI Text element with the `HealthDisplay` script
3. Call `TakeDamage()` from the inspector or another script
4. Watch the UI update automatically!

## What Just Happened?

1. **Event Definition**: We created a simple data class to carry information
2. **Event Subscription**: The `HealthDisplay` registered to receive health events
3. **Event Publishing**: The `PlayerController` sent events when health changed
4. **Automatic Handling**: The EventBus delivered the event to the handler

## Key Benefits

- **No Direct References**: `PlayerController` doesn't need to know about `HealthDisplay`
- **Multiple Subscribers**: You can add more handlers without changing existing code
- **Thread Safe**: Events are processed safely on Unity's main thread
- **Easy to Extend**: Add new event types and handlers as needed

## Next Steps

Now that you have a basic event system working, let's learn about the core concepts:

**➡️ Continue to [03-Core Concepts](03-Core-Concepts)**

---

## Common Questions

**Q: Do I need to clean up handlers?**
A: For simple cases like this, Unity will clean up when the GameObject is destroyed. For more complex scenarios, see the [Best Practices](Best-Practices) guide.

**Q: Can I have multiple handlers for the same event?**
A: Yes! You can register as many handlers as you want for the same event type.

**Q: What if I send an event before any handlers are registered?**
A: The event will be processed as soon as a handler is registered. For more control, see [Event Buffering](Event-Buffering).