using System;
using System.Threading.Tasks;

namespace com.DvosTools.bus
{
    public interface IDispatcher
    {
        void Dispatch(Action? action, string? eventTypeName = null, Guid? aggregateId = null);
        void DispatchAndWait(Action? action, string? eventTypeName = null, Guid? aggregateId = null);
        Task DispatchAndWaitAsync(Action? action, string? eventTypeName = null, Guid? aggregateId = null);
    }
}