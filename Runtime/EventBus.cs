#nullable enable
using System;
using System.Collections.Generic;
using com.DvosTools.bus.Core;
using com.DvosTools.bus.Dispatchers;

namespace com.DvosTools.bus
{
    /// <summary>
    /// Central event communication hub. Supports class and struct events.
    /// Hot path: SendAndWait + UnityDispatcher main-thread subscriber = zero per-call alloc.
    /// </summary>
    public static class EventBus
    {
        [Obsolete("The instance API is deprecated. Use static methods directly (EventBus.Send, etc.).")]
        public static EventBusInstance Instance => new EventBusInstance();

        // ===== EVENT PUBLISHING =====

        public static void Send<T>(in T eventData, Guid aggregateId = default)
            => EventBusService.Send(in eventData, aggregateId);

        public static void SendAndWait<T>(in T eventData, Guid aggregateId = default)
            => EventBusService.SendAndWait(in eventData, aggregateId);

        // ===== EVENT SUBSCRIPTION =====

        public static void RegisterHandler<T>(Action<T> handler, Guid aggregateId = default, IDispatcher? dispatcher = null)
            => EventBusService.RegisterHandler(handler, aggregateId, dispatcher);

        public static void RegisterUnityHandler<T>(Action<T> handler, Guid aggregateId = default)
        {
            try { RegisterHandler(handler, aggregateId, UnityDispatcher.Instance); }
            catch (Exception ex)
            {
                throw new InvalidOperationException("UnityDispatcher is not available. Make sure you're running in a Unity environment.", ex);
            }
        }

        public static void RegisterBackgroundHandler<T>(Action<T> handler, Guid aggregateId = default)
            => RegisterHandler(handler, aggregateId, new ThreadPoolDispatcher());

        public static void RegisterRoutedHandler<T>(Action<T> handler, Guid aggregateId, IDispatcher? dispatcher = null)
        {
            if (aggregateId == Guid.Empty)
                throw new ArgumentException("Aggregate ID cannot be empty for routed handlers", nameof(aggregateId));
            RegisterHandler(handler, aggregateId, dispatcher);
            AggregateReady(aggregateId, handler);
        }

        public static void RegisterGlobalHandler<T>(Action<T> handler, IDispatcher? dispatcher = null)
            => EventBusService.RegisterHandler(handler, Guid.Empty, dispatcher);

        [Obsolete("Use RegisterGlobalHandler or RegisterHandler instead.")]
        public static void RegisterStaticHandler<T>(Action<T> handler, IDispatcher? dispatcher = null)
            => EventBusService.RegisterHandler(handler, Guid.Empty, dispatcher);

        // ===== LIFECYCLE =====

        public static void DisposeHandlers<T>() => EventBusService.DisposeHandlers<T>();

        public static void DisposeAllHandlers() => EventBusCore.ClearAll();

        public static void ClearEventsForAggregate(Guid aggregateId) => EventBusCore.ClearEventsForAggregate(aggregateId);

        public static void ResetAggregate(Guid aggregateId) => EventBusCore.ResetAggregate(aggregateId);

        public static void DisposeHandlersForAggregate<T>(Guid aggregateId) => EventBusService.DisposeHandlersForAggregate<T>(aggregateId);

        public static void DisposeHandlerFromAggregate<T>(Action<T> handler, Guid aggregateId) => EventBusService.DisposeHandlerFromAggregate(handler, aggregateId);

        public static int GetHandlerCountForAggregate(Guid aggregateId) => EventBusCore.GetHandlerCountForAggregate(aggregateId);

        public static bool HasHandlersForAggregate(Guid aggregateId) => EventBusCore.HasHandlersForAggregate(aggregateId);

        public static void ClearAll() => EventBusCore.ClearAll();

        public static void AggregateReady(Guid aggregateId) => EventBusService.AggregateReady(aggregateId);

        public static void AggregateReady(Guid aggregateId, params Delegate[] handlers) => EventBusService.AggregateReady(aggregateId, handlers);

        public static int GetBufferedEventCount(Guid aggregateId) => EventBusCore.GetBufferedEventCount(aggregateId);

        public static IEnumerable<Guid> GetBufferedAggregateIds() => EventBusCore.GetBufferedAggregateIds();

        public static int GetTotalBufferedEventCount() => EventBusCore.GetTotalBufferedEventCount();

        public static int GetQueueCount() => EventBusCore.GetTotalQueueCount();

        public static int GetHandlerCount<T>() => EventBusCore.GetHandlerCount<T>();

        public static bool HasHandlers<T>() => GetHandlerCount<T>() > 0;

        public static void Shutdown() => EventBusCore.Shutdown();

        public static void Cleanup()
        {
            try
            {
                BusHelper.Instance?.Cleanup();
                UnityDispatcher.Instance?.Cleanup();
            }
            catch { /* ignored */ }
            EventBusCore.Dispose();
        }

        public static bool HasBufferedEvents() => GetTotalBufferedEventCount() > 0;

        public static bool HasQueuedEvents() => GetQueueCount() > 0;
    }

    /// <summary>[Obsolete] Wrapper for backwards compat with the instance API.</summary>
    [Obsolete("The instance API is deprecated.")]
    public class EventBusInstance
    {
        [Obsolete("Use EventBus.Send().")] public void Send<T>(T eventData) => EventBus.Send(eventData);
        [Obsolete("Use EventBus.SendAndWait().")] public void SendAndWait<T>(T eventData) => EventBus.SendAndWait(eventData);
        [Obsolete("Use EventBus.AggregateReady().")] public void AggregateReady(Guid aggregateId) => EventBus.AggregateReady(aggregateId);
        [Obsolete("Use EventBus.AggregateReady().")] public void AggregateReady(Guid aggregateId, params Delegate[] handlers) => EventBus.AggregateReady(aggregateId, handlers);
        [Obsolete("Use EventBus.GetBufferedEventCount().")] public int GetBufferedEventCount(Guid aggregateId) => EventBus.GetBufferedEventCount(aggregateId);
        [Obsolete("Use EventBus.GetBufferedAggregateIds().")] public IEnumerable<Guid> GetBufferedAggregateIds() => EventBus.GetBufferedAggregateIds();
        [Obsolete("Use EventBus.GetTotalBufferedEventCount().")] public int GetTotalBufferedEventCount() => EventBus.GetTotalBufferedEventCount();
        [Obsolete("Use EventBus.GetQueueCount().")] public int GetQueueCount() => EventBus.GetQueueCount();
        [Obsolete("Use EventBus.GetHandlerCount<T>().")] public int GetHandlerCount<T>() => EventBus.GetHandlerCount<T>();
        [Obsolete("Use EventBus.HasHandlers<T>().")] public bool HasHandlers<T>() => EventBus.HasHandlers<T>();
        [Obsolete("Use EventBus.RegisterGlobalHandler.")]
        public void RegisterStaticHandler<T>(Action<T> handler, IDispatcher? dispatcher = null) => EventBus.RegisterGlobalHandler(handler, dispatcher);
        [Obsolete("Use EventBus.Shutdown().")] public void Shutdown() => EventBus.Shutdown();

        [Obsolete("Direct handlers access is removed; this returns an empty snapshot.")]
        public Dictionary<Type, List<Subscription>> Handlers => new();

        [Obsolete("Direct EventQueue access is removed; this returns an empty queue.")]
        public Queue<QueuedEvent> EventQueue => new();

        [Obsolete("QueueLock now returns the internal scheduler enqueue lock; holding it pauses Send/queue drain.")]
        public object QueueLock => Core.QueueScheduler.EnqueueLock;
    }
}
