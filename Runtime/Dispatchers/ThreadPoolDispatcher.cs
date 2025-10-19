#nullable enable
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace com.DvosTools.bus.Dispatchers
{
    public class ThreadPoolDispatcher : IDispatcher
    {
        private static readonly ConcurrentQueue<Action> ActionQueue = new();
        private static readonly object Lock = new();
        private static bool _isProcessing;

        public void Dispatch(Action? action, string? eventTypeName = null, Guid? aggregateId = null)
        {
            if (action != null)
            {
                try
                {
                    // Queue the action for sequential processing
                    ActionQueue.Enqueue(() =>
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
                    });
                    
                    // Start processing if not already running
                    StartProcessingIfNeeded();
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

        private static void StartProcessingIfNeeded()
        {
            lock (Lock)
            {
                if (!_isProcessing)
                {
                    _isProcessing = true;
                    Task.Run(ProcessActionQueue);
                }
            }
        }

        private static void ProcessActionQueue()
        {
            try
            {
                var processedCount = 0;
                // Process all queued actions in order
                while (ActionQueue.TryDequeue(out var action))
                {
                    try
                    {
                        action?.Invoke();
                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        EventBusLogger.LogError($"Error executing action on thread pool: {ex.Message}");
                    }
                }
                
                if (processedCount > 0)
                {
                    EventBusLogger.Log($"ThreadPoolDispatcher processed {processedCount} actions");
                }
            }
            finally
            {
                lock (Lock)
                {
                    _isProcessing = false;
                    // If more actions were queued while processing, start again
                    if (!ActionQueue.IsEmpty)
                    {
                        _isProcessing = true;
                        Task.Run(ProcessActionQueue);
                    }
                }
            }
        }
    }
}