using System;

namespace com.DvosTools.bus
{
    public interface IDispatcher
    {
        void Dispatch(Action? action);
        void DispatchAndWait(Action? action);
    }
}