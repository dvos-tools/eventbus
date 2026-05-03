using System;
using System.Threading.Tasks;

namespace com.DvosTools.bus.Dispatchers
{
    public class ThreadPoolDispatcher : IDispatcher
    {
        private sealed class WorkItem
        {
            public Action<object>? Handler;
            public object? State;
            public string? EventTypeName;
            public Guid? AggregateId;
        }

        private static readonly Action<object> RunWorkItem = obj =>
        {
            var w = (WorkItem)obj;
            try
            {
                w.Handler!(w.State!);
            }
            catch (Exception ex)
            {
                var errorMessage = w.AggregateId.HasValue
                    ? $"Routed handler error for {w.EventTypeName} (ID: {w.AggregateId}): {ex.Message}"
                    : $"Handler error for {w.EventTypeName}: {ex.Message}";
                EventBusLogger.LogError(errorMessage);
            }
        };

        public void Dispatch(Action<object> handler, object state, string? eventTypeName = null, Guid? aggregateId = null)
        {
            if (handler == null) return;
            Task.Factory.StartNew(RunWorkItem, Box(handler, state, eventTypeName, aggregateId));
        }

        public void DispatchAndWait(Action<object> handler, object state, string? eventTypeName = null, Guid? aggregateId = null)
        {
            if (handler == null) return;
            Task.Factory.StartNew(RunWorkItem, Box(handler, state, eventTypeName, aggregateId)).Wait();
        }

        public Task DispatchAndWaitAsync(Action<object> handler, object state, string? eventTypeName = null, Guid? aggregateId = null)
        {
            if (handler == null) return Task.CompletedTask;
            return Task.Factory.StartNew(RunWorkItem, Box(handler, state, eventTypeName, aggregateId));
        }

        private static WorkItem Box(Action<object> handler, object state, string? eventTypeName, Guid? aggregateId)
        {
            return new WorkItem
            {
                Handler = handler,
                State = state,
                EventTypeName = eventTypeName,
                AggregateId = aggregateId
            };
        }
    }
}
