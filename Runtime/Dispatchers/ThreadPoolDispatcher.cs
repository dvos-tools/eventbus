using System;
using System.Threading.Tasks;

namespace com.DvosTools.bus.Dispatchers
{
    public class ThreadPoolDispatcher : IDispatcher
    {
        public void Dispatch(Action? action, string? eventTypeName = null, Guid? aggregateId = null)
        {
            if (action != null)
            {
                try
                {
                    Task.Run(action);
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

        public void DispatchAndWait(Action? action, string? eventTypeName = null, Guid? aggregateId = null)
        {
            if (action != null)
            {
                try
                {
                    Task.Run(action).Wait();
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

        public async Task DispatchAndWaitAsync(Action? action, string? eventTypeName = null, Guid? aggregateId = null)
        {
            if (action != null)
            {
                try
                {
                    await Task.Run(action);
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
}