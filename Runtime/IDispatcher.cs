using System;
using System.Threading.Tasks;

namespace com.DvosTools.bus
{
    /// <summary>
    /// Dispatchers receive a handler delegate and an opaque state object, then invoke
    /// <c>handler(state)</c> on whatever thread/scheduler the dispatcher targets.
    /// Passing handler+state explicitly lets the bus avoid allocating a new closure per dispatch.
    /// </summary>
    public interface IDispatcher
    {
        void Dispatch(Action<object> handler, object state, string? eventTypeName = null, Guid? aggregateId = null);
        void DispatchAndWait(Action<object> handler, object state, string? eventTypeName = null, Guid? aggregateId = null);
        Task DispatchAndWaitAsync(Action<object> handler, object state, string? eventTypeName = null, Guid? aggregateId = null);
    }
}
