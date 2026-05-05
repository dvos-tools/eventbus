using System;
using System.Threading.Tasks;

namespace com.DvosTools.bus
{
    /// <summary>
    /// Dispatchers receive a typed handler delegate and a state value, then invoke
    /// handler(state) on whatever thread/scheduler the dispatcher targets.
    /// `in T` keeps large structs from copying at the call boundary.
    /// </summary>
    public interface IDispatcher
    {
        void Dispatch<T>(Action<T> handler, in T state, string? eventTypeName = null, Guid? aggregateId = null);
        void DispatchAndWait<T>(Action<T> handler, in T state, string? eventTypeName = null, Guid? aggregateId = null);
        Task DispatchAndWaitAsync<T>(Action<T> handler, in T state, string? eventTypeName = null, Guid? aggregateId = null);
    }
}
