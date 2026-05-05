#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace com.DvosTools.bus.Core
{
    internal interface IDrainable { Task DrainOneAsync(); }

    /// <summary>
    /// Single global FIFO arbiter. Send&lt;T&gt; takes _enqueueLock, enqueues to per-T queue,
    /// then enqueues TypedDrainable&lt;T&gt;.Instance to _ready. Worker dequeues drainables in arrival order;
    /// each drainable pops one event from its per-T queue. Same lock around both ops = strict cross-type FIFO.
    /// </summary>
    internal static class QueueScheduler
    {
        private static readonly Queue<IDrainable> _ready = new();
        private static readonly object _enqueueLock = new();
        private static SemaphoreSlim _signal = new(0);
        private static CancellationTokenSource _cts = new();
        private static Task? _workerTask;

        public static object EnqueueLock => _enqueueLock;

        public static void Enqueue<T>(in QueuedEvent<T> ev)
        {
            lock (_enqueueLock)
            {
                lock (EventQueueStore<T>.Lock) EventQueueStore<T>.Queue.Enqueue(ev);
                _ready.Enqueue(TypedDrainable<T>.Instance);
            }
            try { _signal.Release(); }
            catch (ObjectDisposedException) { }
            catch (SemaphoreFullException) { }
        }

        public static void Start()
        {
            if (_workerTask != null && !_workerTask.IsCompleted) return;
            _cts = new CancellationTokenSource();
            _workerTask = Task.Run(RunWorker, _cts.Token);
        }

        public static void Shutdown()
        {
            try { if (!_cts.IsCancellationRequested) _cts.Cancel(); }
            catch (ObjectDisposedException) { }
        }

        public static void Reset()
        {
            Shutdown();
            try { _cts.Dispose(); } catch (ObjectDisposedException) { }
            // Keep _signal across resets to avoid races with old worker still parked in WaitAsync.
            _cts = new CancellationTokenSource();
            _workerTask = Task.Run(RunWorker, _cts.Token);
        }

        public static int ReadyCount()
        {
            lock (_enqueueLock) return _ready.Count;
        }

        public static void ClearReady()
        {
            lock (_enqueueLock) _ready.Clear();
        }

        private static async Task RunWorker()
        {
            var token = _cts.Token;
            while (!token.IsCancellationRequested)
            {
                try { await _signal.WaitAsync(token); }
                catch (OperationCanceledException) { break; }
                catch (ObjectDisposedException) { break; }

                IDrainable? drain = null;
                lock (_enqueueLock) { if (_ready.Count > 0) drain = _ready.Dequeue(); }

                if (drain != null)
                {
                    try { await drain.DrainOneAsync(); }
                    catch (Exception ex)
                    {
                        if (token.IsCancellationRequested) break;
                        EventBusLogger.LogError($"Queue processor error: {ex.Message}");
                    }
                }
            }
        }
    }

    internal sealed class TypedDrainable<T> : IDrainable
    {
        public static readonly TypedDrainable<T> Instance = new();

        public async Task DrainOneAsync()
        {
            QueuedEvent<T> ev;
            lock (EventQueueStore<T>.Lock)
            {
                if (EventQueueStore<T>.Queue.Count == 0) return;
                ev = EventQueueStore<T>.Queue.Dequeue();
            }

            Subscription<T>[]? globals;
            Subscription<T>[]? routed = null;
            lock (HandlerStore<T>.Lock)
            {
                globals = HandlerStore<T>.GlobalSnapshot;
                if (ev.AggregateId != Guid.Empty)
                    HandlerStore<T>.RoutedSnapshot.TryGetValue(ev.AggregateId, out routed);
            }

            if (globals == null && routed == null)
            {
                EventBusLogger.LogWarning($"No handlers for {EventTypeName<T>.Value}");
                return;
            }

            Guid? id = ev.AggregateId == Guid.Empty ? (Guid?)null : ev.AggregateId;

            if (globals != null)
                foreach (var sub in globals)
                    await sub.Dispatcher.DispatchAndWaitAsync(sub.Handler, ev.Event, EventTypeName<T>.Value, id);
            if (routed != null)
                foreach (var sub in routed)
                    await sub.Dispatcher.DispatchAndWaitAsync(sub.Handler, ev.Event, EventTypeName<T>.Value, id);
        }
    }
}
