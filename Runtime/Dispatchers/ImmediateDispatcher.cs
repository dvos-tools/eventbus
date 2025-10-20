using System;
using System.Threading.Tasks;

namespace com.DvosTools.bus.Dispatchers
{
    public class ImmediateDispatcher : IDispatcher
    {
        public void Dispatch(Action? action, string? eventTypeName = null, Guid? aggregateId = null)
        {
            if (action != null)
            {
                try
                {
                    action.Invoke();
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
                    action.Invoke();
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
                    action.Invoke();
                    await Task.CompletedTask; // For consistency with async signature
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