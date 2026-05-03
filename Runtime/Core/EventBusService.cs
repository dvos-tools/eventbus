#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using com.DvosTools.bus.Dispatchers;

namespace com.DvosTools.bus.Core
{
    internal class EventBusService
    {
        private readonly EventBusCore _core;

        public EventBusService(EventBusCore core)
        {
            _core = core ?? throw new ArgumentNullException(nameof(core));
        }

        public void Send<T>(T eventData) where T : class
        {
            var queuedEvent = new QueuedEvent(eventData, typeof(T), DateTime.UtcNow);
            var aggregateId = queuedEvent.AggregateId;

            // Handle non-routed events (no aggregate ID)
            if (aggregateId == Guid.Empty)
            {
                QueueEvent(queuedEvent, typeof(T).Name);
                return;
            }

            // Handle routed events - check if handler exists
            if (HasHandlerForAggregate(typeof(T), aggregateId))
            {
                QueueEvent(queuedEvent, typeof(T).Name);
                return;
            }

            // No handler found - buffer the event
            BufferEvent(queuedEvent, aggregateId, typeof(T).Name);
        }

        public void SendAndWait<T>(T eventData) where T : class
        {
            var eventType = typeof(T);
            var aggregateId = eventData is IRoutableEvent r ? r.AggregateId : Guid.Empty;

            var handlerInfos = SnapshotHandlersFor(eventType, aggregateId);
            if (handlerInfos.Count == 0)
            {
                EventBusLogger.LogWarning($"No handlers for {eventType.Name}");
                return;
            }

            // Process handlers sequentially to maintain FIFO order
            foreach (var handlerInfo in handlerInfos)
            {
                ProcessHandlerAndWaitAsync(handlerInfo, eventData, eventType.Name, aggregateId).Wait();
            }
        }

        public void AggregateReady(Guid aggregateId, IReadOnlyCollection<Delegate>? handlersForReadyFlush = null)
        {
            if (aggregateId == Guid.Empty)
            {
                EventBusLogger.LogWarning("Cannot mark empty aggregate ID as ready");
                return;
            }

            if (handlersForReadyFlush != null)
            {
                if (handlersForReadyFlush.Count == 0)
                {
                    EventBusLogger.LogWarning("AggregateReady with an empty handler list does nothing; use AggregateReady(aggregateId) without handlers to flush the entire buffer.");
                    return;
                }

                AggregateReadyForHandlers(aggregateId, handlersForReadyFlush);
                return;
            }

            var eventsToProcess = ExtractBufferedEvents(aggregateId);
            if (eventsToProcess.Count == 0)
            {
                EventBusLogger.Log($"No buffered events found for aggregate ID {aggregateId}");
                return;
            }

            // Process buffered events immediately, and in order
            // This ensures they are processed as a batch in the exact order they were buffered
            foreach (var eventToProcess in eventsToProcess)
            {
                ProcessEventImmediately(eventToProcess);
            }

            EventBusLogger.Log($"Processed {eventsToProcess.Count} buffered events immediately (aggregate ID: {aggregateId})");
        }

        private void AggregateReadyForHandlers(Guid aggregateId, IReadOnlyCollection<Delegate> handlers)
        {
            int processedCount = 0;
            lock (_core.BufferedEventsLock)
            {
                if (!_core.BufferedEvents.TryGetValue(aggregateId, out var bufferedQueue))
                {
                    EventBusLogger.Log($"No buffered events found for aggregate ID {aggregateId}");
                    return;
                }

                var remaining = new Queue<QueuedEvent>();
                while (bufferedQueue.Count > 0)
                {
                    var queuedEvent = bufferedQueue.Dequeue();
                    if (ShouldProcessBufferedEventForReadyHandlers(queuedEvent, aggregateId, handlers))
                    {
                        ProcessEventImmediately(queuedEvent);
                        processedCount++;
                    }
                    else
                    {
                        remaining.Enqueue(queuedEvent);
                    }
                }

                if (remaining.Count > 0)
                    _core.BufferedEvents[aggregateId] = remaining;
                else
                    _core.BufferedEvents.Remove(aggregateId);
            }

            EventBusLogger.Log($"Processed {processedCount} buffered events for specified handlers (aggregate ID: {aggregateId})");
        }

        /// <summary>
        /// Partial flush: only dequeue buffered events that correspond to one of the given handler
        /// delegate references. Each released event is delivered via <see cref="ProcessEventImmediately"/>,
        /// matching the full AggregateReady path (not a separate filtered delivery).
        /// </summary>
        private bool ShouldProcessBufferedEventForReadyHandlers(QueuedEvent queuedEvent, Guid aggregateId, IReadOnlyCollection<Delegate> handlers)
        {
            lock (_core.HandlersLock)
            {
                if (!_core.Handlers.TryGetValue(queuedEvent.EventType, out var byAggregate))
                    return false;
                return byAggregate.TryGetValue(aggregateId, out var list) && list.Count > 0;
            }
        }

        public async Task ProcessEventAsync(QueuedEvent queuedEvent)
        {
            var handlerInfos = SnapshotHandlersFor(queuedEvent.EventType, queuedEvent.AggregateId);
            if (handlerInfos.Count == 0)
            {
                EventBusLogger.LogWarning($"No handlers for {queuedEvent.EventType.Name}");
                return;
            }

            // Process handlers sequentially to maintain FIFO order
            foreach (var handlerInfo in handlerInfos)
            {
                await ProcessHandlerAndWaitAsync(handlerInfo, queuedEvent.EventData, queuedEvent.EventType.Name, queuedEvent.AggregateId);
            }
        }


        public static void RegisterHandler<T>(Action<T> handler, Guid aggregateId = default, IDispatcher? dispatcher = null) where T : class
        {
            var core = EventBusCore.Instance;
            var eventType = typeof(T);
            var wrapper = new Action<object>(obj => handler((T)obj));

            var customDispatcher = dispatcher ?? new ThreadPoolDispatcher();
            var subscription = new Subscription(wrapper, customDispatcher, aggregateId, handler);
            EventBusLogger.Log($"Registered handler: {handler?.GetHashCode()} for aggregate {aggregateId}");

            lock (core.HandlersLock)
            {
                if (!core.Handlers.TryGetValue(eventType, out var byAggregate))
                {
                    byAggregate = new Dictionary<Guid, List<Subscription>>();
                    core.Handlers[eventType] = byAggregate;
                }
                if (!byAggregate.TryGetValue(aggregateId, out var list))
                {
                    list = new List<Subscription>();
                    byAggregate[aggregateId] = list;
                }
                list.Add(subscription);
            }

            EventBusLogger.Log(aggregateId != Guid.Empty
                ? $"Registered routed handler for {eventType.Name} (ID: {aggregateId})"
                : $"Registered handler for {eventType.Name}");
        }


        private bool HasHandlerForAggregate(Type eventType, Guid aggregateId)
        {
            lock (_core.HandlersLock)
            {
                if (!_core.Handlers.TryGetValue(eventType, out var byAggregate))
                    return false;
                return byAggregate.TryGetValue(aggregateId, out var list) && list.Count > 0;
            }
        }

        /// <summary>
        /// Snapshot the subscriptions that should receive an event of <paramref name="eventType"/>:
        /// global handlers (Guid.Empty bucket) plus, when the event is routed, the matching aggregate bucket.
        /// Snapshot is taken under HandlersLock so dispatch can run lock-free.
        /// </summary>
        private List<Subscription> SnapshotHandlersFor(Type eventType, Guid eventAggregateId)
        {
            var result = new List<Subscription>();
            lock (_core.HandlersLock)
            {
                if (!_core.Handlers.TryGetValue(eventType, out var byAggregate))
                    return result;

                if (byAggregate.TryGetValue(Guid.Empty, out var globals))
                    result.AddRange(globals);

                if (eventAggregateId != Guid.Empty
                    && byAggregate.TryGetValue(eventAggregateId, out var routed))
                {
                    result.AddRange(routed);
                }
            }
            return result;
        }


        private void QueueEvent(QueuedEvent queuedEvent, string eventTypeName)
        {
            lock (_core.QueueLock)
                _core.EventQueue.Enqueue(queuedEvent);

            _core.NotifyQueued();
            EventBusLogger.Log($"Queued {eventTypeName}");
        }

        private void BufferEvent(QueuedEvent queuedEvent, Guid aggregateId, string eventTypeName)
        {
            lock (_core.BufferedEventsLock)
            {
                if (!_core.BufferedEvents.ContainsKey(aggregateId))
                    _core.BufferedEvents[aggregateId] = new Queue<QueuedEvent>();

                _core.BufferedEvents[aggregateId].Enqueue(queuedEvent);
            }

            EventBusLogger.Log($"Buffered {eventTypeName} for aggregate ID {aggregateId} (no handler available)");
        }

        private Task ProcessHandlerAndWaitAsync(Subscription subscription, object eventData, string eventTypeName, Guid eventAggregateId)
        {
            // Bucket selection guarantees subscription is either global (Guid.Empty) or matches eventAggregateId.
            // Pass handler+state directly so dispatcher does not allocate a closure per call.
            if (subscription.AggregateId != Guid.Empty)
                return subscription.Dispatcher.DispatchAndWaitAsync(subscription.Handler, eventData, eventTypeName, eventAggregateId);

            return subscription.Dispatcher.DispatchAndWaitAsync(subscription.Handler, eventData, eventTypeName);
        }

        public void ProcessEventImmediately(QueuedEvent queuedEvent)
        {
            var handlerInfos = SnapshotHandlersFor(queuedEvent.EventType, queuedEvent.AggregateId);
            if (handlerInfos.Count == 0)
            {
                EventBusLogger.LogWarning($"No handlers for {queuedEvent.EventType.Name}");
                return;
            }

            // Process handlers immediately and sequentially to maintain FIFO order
            foreach (var handlerInfo in handlerInfos)
            {
                ProcessHandlerImmediately(handlerInfo, queuedEvent.EventData, queuedEvent.EventType.Name, queuedEvent.AggregateId);
            }
        }

        private void ProcessHandlerImmediately(Subscription subscription, object eventData, string eventTypeName, Guid eventAggregateId)
        {
            if (subscription.AggregateId != Guid.Empty)
            {
                subscription.Dispatcher.DispatchAndWait(subscription.Handler, eventData, eventTypeName, eventAggregateId);
                return;
            }

            subscription.Dispatcher.DispatchAndWait(subscription.Handler, eventData, eventTypeName);
        }


        private List<QueuedEvent> ExtractBufferedEvents(Guid aggregateId)
        {
            lock (_core.BufferedEventsLock)
            {
                if (!_core.BufferedEvents.TryGetValue(aggregateId, out var bufferedQueue))
                    return new List<QueuedEvent>();

                var eventsToProcess = new List<QueuedEvent>();
                while (bufferedQueue.Count > 0)
                {
                    eventsToProcess.Add(bufferedQueue.Dequeue());
                }

                _core.BufferedEvents.Remove(aggregateId);
                return eventsToProcess;
            }
        }

        public void DisposeHandlerFromAggregate<T>(Action<T> handler, Guid aggregateId) where T : class
        {
            lock (_core.QueueLock)
            {
                _core.DisposeHandlerFromAggregate(handler, aggregateId);
            }
        }

    }
}
