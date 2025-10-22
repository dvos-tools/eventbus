#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace com.DvosTools.bus.Core
{
    internal class EventBusCore
    {
        private static EventBusCore? _instance;
        private static EventBusService? _service;
        
        public readonly Dictionary<Type, List<Subscription>> Handlers = new();
        public readonly Queue<QueuedEvent> EventQueue = new();
        public readonly Dictionary<Guid, Queue<QueuedEvent>> BufferedEvents = new();
        public readonly object QueueLock = new();
        public readonly object BufferedEventsLock = new();
        public readonly object HandlersLock = new();
        private CancellationTokenSource _cancellationTokenSource;
        
        

        public static EventBusCore Instance => _instance ??= new EventBusCore();
        public static EventBusService Service => _service ??= new EventBusService(Instance);

        private EventBusCore()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _ = Task.Run(ProcessEventQueueAsync, _cancellationTokenSource.Token); // Start the background queue processor
        }

        private async Task ProcessEventQueueAsync()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    QueuedEvent? eventToProcess = null;
                    
                    // Get one event at a time
                    lock (QueueLock)
                    {
                        if (EventQueue.Count > 0)
                            eventToProcess = EventQueue.Dequeue();
                    }

                    if (eventToProcess != null)
                    {
                        await Service.ProcessEventAsync(eventToProcess);
                    }
                    else
                    {
                        await Task.Yield();
                    }
                }
                catch (OperationCanceledException)
                {
                    break; // Expected when cancellation is requested
                }
                catch (Exception ex)
                {
                    EventBusLogger.LogError($"Queue processor error: {ex.Message}");
                }
            }
        }

        public int GetBufferedEventCount(Guid aggregateId)
        {
            lock (BufferedEventsLock)
            {
                return BufferedEvents.TryGetValue(aggregateId, out var bufferedQueue) ? bufferedQueue.Count : 0;
            }
        }

        public IEnumerable<Guid> GetBufferedAggregateIds()
        {
            lock (BufferedEventsLock)
            {
                return BufferedEvents.Keys.ToList();
            }
        }

        public int GetTotalBufferedEventCount()
        {
            lock (BufferedEventsLock)
            {
                return BufferedEvents.Values.Sum(queue => queue.Count);
            }
        }

        public void Shutdown()
        {
            try
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                }
            }
            catch (ObjectDisposedException)
            {
                // Already disposed, ignore
            }
        }

        public void ClearAll()
        {
            // Clear handlers
            lock (HandlersLock)
            {
                Handlers.Clear();
            }
            
            // Clear buffered events
            lock (BufferedEventsLock)
            {
                BufferedEvents.Clear();
            }
            
            // Clear event queue
            lock (QueueLock)
            {
                EventQueue.Clear();
            }
            
            // Reset the background task to ensure it's running properly
            ResetBackgroundTask();
        }

        public void ResetBackgroundTask()
        {
            // Cancel the current background task
            Shutdown();
            
            // Create a new CancellationTokenSource and restart the background task
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            _ = Task.Run(ProcessEventQueueAsync, _cancellationTokenSource.Token);
        }

        public void Dispose()
        {
            // Cancel the background task
            Shutdown();
            
            // Dispose the cancellation token source
            _cancellationTokenSource.Dispose();
            
            // Clear all collections to free memory
            lock (HandlersLock)
            {
                Handlers.Clear();
            }
            
            lock (BufferedEventsLock)
            {
                BufferedEvents.Clear();
            }
            
            lock (QueueLock)
            {
                EventQueue.Clear();
            }
        }

    }
}