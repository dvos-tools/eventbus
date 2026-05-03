using System;
using System.Threading.Tasks;

namespace com.DvosTools.bus.Dispatchers
{
    public class ImmediateDispatcher : IDispatcher
    {
        public void Dispatch(Action<object> handler, object state, string? eventTypeName = null, Guid? aggregateId = null)
        {
            Invoke(handler, state, eventTypeName, aggregateId);
        }

        public void DispatchAndWait(Action<object> handler, object state, string? eventTypeName = null, Guid? aggregateId = null)
        {
            Invoke(handler, state, eventTypeName, aggregateId);
        }

        public Task DispatchAndWaitAsync(Action<object> handler, object state, string? eventTypeName = null, Guid? aggregateId = null)
        {
            Invoke(handler, state, eventTypeName, aggregateId);
            return Task.CompletedTask;
        }

        private static void Invoke(Action<object> handler, object state, string? eventTypeName, Guid? aggregateId)
        {
            if (handler == null) return;
            try
            {
                handler(state);
            }
            catch (Exception ex)
            {
                var errorMessage = aggregateId.HasValue
                    ? $"Routed handler error for {eventTypeName} (ID: {aggregateId}): {ex.Message}"
                    : $"Handler error for {eventTypeName}: {ex.Message}";
                EventBusLogger.LogError(errorMessage);
            }
        }
    }
}
