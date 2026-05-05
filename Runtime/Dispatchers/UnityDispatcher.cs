#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace com.DvosTools.bus.Dispatchers
{
    public class UnityDispatcher : MonoBehaviour, IDispatcher
    {
        /// <summary>Max main-thread jobs drained per Unity Update tick. Tunable for hot paths.</summary>
        public static int DrainBudgetPerFrame { get; set; } = 32;

        private static UnityDispatcher? _instance;
        private static readonly object Lock = new();
        private static SynchronizationContext? _mainThreadContext;
        private static readonly Queue<IRunnable> _actionQueue = new();
        private static readonly object _queueLock = new();

        public static UnityDispatcher? Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Lock)
                    {
                        if (_instance == null)
                        {
                            var go = new GameObject(nameof(UnityDispatcher));
                            _instance = go.AddComponent<UnityDispatcher>();
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
            }
        }

        private void Awake() { _mainThreadContext = SynchronizationContext.Current; }

        public bool IsQueueEmpty()
        {
            lock (_queueLock) return _actionQueue.Count == 0;
        }

        public void Dispatch<T>(Action<T> handler, in T state, string? eventTypeName = null, Guid? aggregateId = null)
        {
            if (handler == null) return;
            var job = new MainThreadJob<T> { Handler = handler, State = state, EventTypeName = eventTypeName, AggregateId = aggregateId };
            lock (_queueLock) _actionQueue.Enqueue(job);
        }

        public void DispatchAndWait<T>(Action<T> handler, in T state, string? eventTypeName = null, Guid? aggregateId = null)
        {
            if (handler == null) return;
            try
            {
                if (SynchronizationContext.Current == _mainThreadContext)
                {
                    handler(state);
                    return;
                }
                var job = new MainThreadJob<T>
                {
                    Handler = handler,
                    State = state,
                    Tcs = new TaskCompletionSource<bool>(),
                    EventTypeName = eventTypeName,
                    AggregateId = aggregateId
                };
                lock (_queueLock) _actionQueue.Enqueue(job);
                job.Tcs!.Task.Wait();
            }
            catch (Exception ex)
            {
                EventBusLogger.LogError(FormatError("DispatchAndWait", eventTypeName, aggregateId, ex));
            }
        }

        public Task DispatchAndWaitAsync<T>(Action<T> handler, in T state, string? eventTypeName = null, Guid? aggregateId = null)
        {
            if (handler == null) return Task.CompletedTask;
            try
            {
                if (SynchronizationContext.Current == _mainThreadContext)
                {
                    handler(state);
                    return Task.CompletedTask;
                }
                var job = new MainThreadJob<T>
                {
                    Handler = handler,
                    State = state,
                    Tcs = new TaskCompletionSource<bool>(),
                    EventTypeName = eventTypeName,
                    AggregateId = aggregateId
                };
                lock (_queueLock) _actionQueue.Enqueue(job);
                return job.Tcs!.Task;
            }
            catch (Exception ex)
            {
                EventBusLogger.LogError(FormatError("DispatchAndWaitAsync", eventTypeName, aggregateId, ex));
                return Task.FromException(ex);
            }
        }

        private static string FormatError(string scope, string? eventTypeName, Guid? aggregateId, Exception ex)
        {
            return aggregateId.HasValue
                ? $"{scope} error for {eventTypeName} (ID: {aggregateId}): {ex.Message}"
                : $"{scope} error for {eventTypeName}: {ex.Message}";
        }

        private void Update()
        {
            int budget = DrainBudgetPerFrame;
            for (int i = 0; i < budget; i++)
            {
                IRunnable? job = null;
                lock (_queueLock)
                {
                    if (_actionQueue.Count > 0) job = _actionQueue.Dequeue();
                }
                if (job == null) break;
                job.Run();
            }
        }

        public void Cleanup()
        {
            lock (_queueLock)
            {
                while (_actionQueue.Count > 0)
                {
                    var job = _actionQueue.Dequeue();
                    job.TryCancel();
                }
            }
            _instance = null;
            _mainThreadContext = null;
        }

        private void OnDestroy() { Cleanup(); }

        internal interface IRunnable { void Run(); void TryCancel(); }

        private sealed class MainThreadJob<T> : IRunnable
        {
            public Action<T>? Handler;
            public T State = default!;
            public TaskCompletionSource<bool>? Tcs;
            public string? EventTypeName;
            public Guid? AggregateId;

            public void Run()
            {
                try
                {
                    Handler!(State);
                    Tcs?.SetResult(true);
                }
                catch (Exception ex)
                {
                    EventBusLogger.LogError(AggregateId.HasValue
                        ? $"Main thread action error for {EventTypeName} (ID: {AggregateId}): {ex.Message}"
                        : $"Main thread action error for {EventTypeName}: {ex.Message}");
                    try { Tcs?.SetException(ex); } catch (InvalidOperationException) { /* already set */ }
                }
            }

            public void TryCancel()
            {
                try { if (Tcs != null && !Tcs.Task.IsCompleted) Tcs.SetCanceled(); }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
            }
        }
    }
}
