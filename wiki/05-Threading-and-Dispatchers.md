# Threading and Dispatchers

Understanding threading is crucial for building robust Unity applications. Unity EventBus provides the UnityDispatcher, which is the only fully supported dispatcher.

## Why Threading Matters in Unity

Unity has strict threading requirements:
- **Main Thread Only**: Unity APIs (GameObjects, Components, UI) must be called from the main thread
- **Thread Safety**: Improper threading can cause crashes, data corruption, or unpredictable behavior
- **Responsiveness**: Proper threading keeps your game responsive and smooth

## The UnityDispatcher

| Dispatcher | Thread | Use Case | Unity API Safe | Order Guaranteed |
|------------|--------|----------|----------------|------------------|
| **UnityDispatcher** | Main Thread | All event handling | ✅ Yes | ✅ Yes |

## UnityDispatcher - Main Thread Execution

**When to use:** All event handling in Unity applications

```csharp
// Register a handler that runs on Unity's main thread
EventBus.RegisterUnityHandler<PlayerHealthChangedEvent>(UpdateHealthUI);

void UpdateHealthUI(PlayerHealthChangedEvent evt)
{
    // Safe to access Unity APIs
    healthBar.fillAmount = (float)evt.CurrentHealth / evt.MaxHealth;
    healthText.text = $"{evt.CurrentHealth}/{evt.MaxHealth}";
    
    // Can manipulate GameObjects
    var player = GameObject.FindWithTag("Player");
    if (player != null)
    {
        player.GetComponent<PlayerController>().UpdateHealth(evt.CurrentHealth);
    }
}
```

**Key Features:**
- ✅ **Unity API Safe** - Can access all Unity objects and APIs
- ✅ **FIFO Order** - Events are processed in the order they were sent
- ✅ **Non-blocking** - Doesn't block the calling thread
- ✅ **Frame-based** - Handlers execute one per frame for smooth gameplay
- ✅ **Fully Supported** - This is the only dispatcher that is fully supported

**Threading Considerations:**
- Large numbers of events may take multiple frames to process
- Perfect for all Unity-specific operations
- Handlers execute smoothly without blocking the main thread

## Using UnityDispatcher

### When to Use UnityDispatcher:
- **All event handling** in Unity applications
- Accessing Unity APIs (GameObjects, Components, UI)
- Updating UI elements
- Manipulating Unity objects
- Working with Unity's rendering system
- Audio playback
- Any Unity-specific operations

```csharp
// UI updates
EventBus.RegisterUnityHandler<PlayerHealthChangedEvent>(UpdateHealthBar);

// GameObject manipulation
EventBus.RegisterUnityHandler<PlayerMovedEvent>(UpdatePlayerPosition);

// Audio playback
EventBus.RegisterUnityHandler<SoundEvent>(PlaySound);

// Any Unity API access
EventBus.RegisterUnityHandler<PlayerEvent>(OnPlayerEvent);
```

### Handler Registration Methods

Unity EventBus provides several convenience methods that all use UnityDispatcher:

```csharp
// Basic registration (uses UnityDispatcher by default)
EventBus.RegisterHandler<PlayerEvent>(OnPlayerEvent);

// Explicit UnityDispatcher registration
EventBus.RegisterUnityHandler<PlayerEvent>(OnPlayerEvent);

// Global handlers (receives all events of this type)
EventBus.RegisterGlobalHandler<GameEvent>(OnGameEvent);

// Routed handlers (receives events for specific aggregate)
EventBus.RegisterRoutedHandler<PlayerEvent>(OnPlayerEvent, playerId);
```

## Common Threading Mistakes

### ❌ Don't: Try to use unsupported dispatchers
```csharp
// WRONG - These dispatchers are not fully supported
EventBus.RegisterBackgroundHandler<PlayerEvent>(OnPlayerEvent); // Not supported
EventBus.RegisterImmediateHandler<PlayerEvent>(OnPlayerEvent); // Not supported
```

### ❌ Don't: Block the main thread with heavy work
```csharp
// WRONG - This will freeze the game
EventBus.RegisterUnityHandler<DataEvent>(evt =>
{
    ProcessHugeDataset(evt.Data); // Freezes the game!
});
```

### ✅ Do: Use UnityDispatcher for all event handling
```csharp
// CORRECT - All event handling with UnityDispatcher
EventBus.RegisterUnityHandler<PlayerEvent>(OnPlayerEvent);
EventBus.RegisterUnityHandler<UIEvent>(UpdateUI);
EventBus.RegisterUnityHandler<GameEvent>(OnGameEvent);
```

### ✅ Do: Keep handlers lightweight
```csharp
// CORRECT - Lightweight handler
EventBus.RegisterUnityHandler<PlayerEvent>(evt =>
{
    // Quick UI update
    UpdatePlayerUI(evt);
    
    // Send heavy work to a coroutine or separate system
    StartCoroutine(ProcessHeavyData(evt.Data));
});
```

## Threading Best Practices

1. **Use UnityDispatcher for all event handling** - it's the only supported dispatcher
2. **Keep handlers lightweight** - avoid heavy computations that could cause frame drops
3. **Use coroutines for heavy work** - move CPU-intensive tasks to coroutines
4. **Avoid blocking the main thread** - use async patterns when possible
5. **Monitor responsiveness** - profile your event handlers

## Debugging Threading Issues

### Check which thread your handler is running on:
```csharp
void MyHandler(MyEvent evt)
{
    Debug.Log($"Handler running on thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
    Debug.Log($"Is main thread: {System.Threading.Thread.CurrentThread == System.Threading.Thread.CurrentThread}");
}
```

### Verify Unity API safety:
```csharp
void MyHandler(MyEvent evt)
{
    // All UnityDispatcher handlers run on the main thread
    Debug.Log("Running on main thread - safe for Unity APIs");
    
    // Safe to access Unity APIs
    var player = GameObject.FindWithTag("Player");
    if (player != null)
    {
        player.transform.position = evt.Position;
    }
}
```

## Next Steps

Now that you understand threading, let's learn about event routing:

**➡️ Continue to [06-Event Routing](06-Event-Routing)**

---

## Quick Reference

| Dispatcher | Method | Thread | Unity APIs | Order | Support Status |
|------------|--------|--------|------------|-------|----------------|
| UnityDispatcher | `RegisterUnityHandler` | Main | ✅ Safe | ✅ FIFO | ✅ Fully Supported |
| ThreadPoolDispatcher | `RegisterBackgroundHandler` | Background | ❌ Unsafe | ❌ Random | ❌ Not Supported |
| ImmediateDispatcher | `RegisterImmediateHandler` | Current | ⚠️ Depends | ✅ FIFO | ❌ Not Supported |