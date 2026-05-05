#nullable enable
using System;
using System.Threading.Tasks;

namespace com.DvosTools.bus.Dispatchers
{
    public class ThreadPoolDispatcher : IDispatcher
    {
        private interface IRunnable { void Run(); }

        private sealed class WorkItem<T> : IRunnable
        {
            public Action<T>? Handler;
            public T State = default!;
            public string? EventTypeName;
            public Guid? AggregateId;

            public void Run()
            {
                try { Handler!(State); }
                catch (Exception ex)
                {
                    EventBusLogger.LogError(AggregateId.HasValue
                        ? $"Routed handler error for {EventTypeName} (ID: {AggregateId}): {ex.Message}"
                        : $"Handler error for {EventTypeName}: {ex.Message}");
                }
            }
        }

        private static readonly Action<object?> RunWorkItem = obj => ((IRunnable)obj!).Run();

        public void Dispatch<T>(Action<T> handler, in T state, string? eventTypeName = null, Guid? aggregateId = null)
        {
            if (handler == null) return;
            var w = new WorkItem<T> { Handler = handler, State = state, EventTypeName = eventTypeName, AggregateId = aggregateId };
            Task.Factory.StartNew(RunWorkItem, w);
        }

        public void DispatchAndWait<T>(Action<T> handler, in T state, string? eventTypeName = null, Guid? aggregateId = null)
        {
            if (handler == null) return;
            var w = new WorkItem<T> { Handler = handler, State = state, EventTypeName = eventTypeName, AggregateId = aggregateId };
            Task.Factory.StartNew(RunWorkItem, w).Wait();
        }

        public Task DispatchAndWaitAsync<T>(Action<T> handler, in T state, string? eventTypeName = null, Guid? aggregateId = null)
        {
            if (handler == null) return Task.CompletedTask;
            var w = new WorkItem<T> { Handler = handler, State = state, EventTypeName = eventTypeName, AggregateId = aggregateId };
            return Task.Factory.StartNew(RunWorkItem, w);
        }
    }
}
