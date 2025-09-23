using System;

namespace com.DvosTools.bus.Runtime
{
    public interface IDispatcher
    {
        void Dispatch(Action? action);
    }
}