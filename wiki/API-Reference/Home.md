# API Reference

Complete reference documentation for all Unity EventBus methods and classes.

## Core API Methods

- **[Event Publishing](Event-Publishing)** - Send and SendAndWait methods
- **[Event Subscription](Event-Subscription)** - Handler registration methods
- **[Handler Management](Handler-Management)** - Cleanup and disposal methods
- **[Aggregate Management](Aggregate-Management)** - Routing and buffering methods
- **[Utility Methods](Utility-Methods)** - Monitoring and debugging methods

## Dispatchers

- **[UnityDispatcher](UnityDispatcher)** - Main thread execution
- **[ThreadPoolDispatcher](ThreadPoolDispatcher)** - Background thread execution
- **[ImmediateDispatcher](ImmediateDispatcher)** - Synchronous execution

## Interfaces

- **[IRoutableEvent](IRoutableEvent)** - Event routing interface
- **[IDispatcher](IDispatcher)** - Dispatcher interface

## Classes

- **[EventBus](EventBus)** - Main static class
- **[EventBusInstance](EventBusInstance)** - Instance API (deprecated)
- **[EventBusLogger](EventBusLogger)** - Logging utilities

## Quick Reference

| Category | Methods | Description |
|----------|---------|-------------|
| **Publishing** | `Send`, `SendAndWait` | Send events to handlers |
| **Subscription** | `RegisterHandler`, `RegisterUnityHandler`, etc. | Subscribe to events |
| **Management** | `DisposeHandlers`, `ClearAll`, etc. | Manage handlers and events |
| **Routing** | `AggregateReady`, `ClearEventsForAggregate` | Handle event routing |
| **Utilities** | `GetQueueCount`, `HasHandlers`, etc. | Monitor and debug |

## Usage Examples

```csharp
// Basic event publishing
EventBus.Send(new PlayerDiedEvent { PlayerName = "Player1" });

// Subscribe to events
EventBus.RegisterUnityHandler<PlayerDiedEvent>(OnPlayerDied);

// Send and wait for completion
EventBus.SendAndWait(new SaveGameEvent { Slot = 1 });

// Clean up handlers
EventBus.DisposeHandlers<PlayerDiedEvent>();
```

## Threading Safety

All EventBus methods are thread-safe and can be called from any thread. However, event handlers run on the thread specified by their dispatcher:

- **UnityDispatcher**: Main thread (Unity APIs safe)
- **ThreadPoolDispatcher**: Background thread (Unity APIs unsafe)
- **ImmediateDispatcher**: Current thread (depends on caller)

## System Considerations

- Use `Send()` for most cases - it's non-blocking
- Use `SendAndWait()` only when you need guaranteed completion
- Choose appropriate dispatchers for your handlers
- Clean up handlers when no longer needed
- Monitor queue sizes for system health