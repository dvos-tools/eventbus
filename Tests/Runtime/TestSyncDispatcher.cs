#nullable enable
using System;
using System.Threading.Tasks;

namespace com.DvosTools.bus
{
    /// <summary>
    /// Test-only dispatcher that invokes the handler synchronously on the calling thread.
    /// Replaces the removed ImmediateDispatcher in tests that need sync semantics with Send().
    /// </summary>
    internal sealed class TestSyncDispatcher : IDispatcher
    {
        public void Dispatch<T>(Action<T> handler, in T state, string? eventTypeName = null, Guid? aggregateId = null)
            => Invoke(handler, state, eventTypeName, aggregateId);

        public void DispatchAndWait<T>(Action<T> handler, in T state, string? eventTypeName = null, Guid? aggregateId = null)
            => Invoke(handler, state, eventTypeName, aggregateId);

        public Task DispatchAndWaitAsync<T>(Action<T> handler, in T state, string? eventTypeName = null, Guid? aggregateId = null)
        {
            Invoke(handler, state, eventTypeName, aggregateId);
            return Task.CompletedTask;
        }

        private static void Invoke<T>(Action<T> handler, in T state, string? eventTypeName, Guid? aggregateId)
        {
            if (handler == null) return;
            try { handler(state); }
            catch (Exception ex) { EventBusLogger.LogError($"TestSyncDispatcher {eventTypeName} (id:{aggregateId}): {ex.Message}"); }
        }
    }
}
