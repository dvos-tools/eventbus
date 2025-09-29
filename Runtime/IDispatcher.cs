using System;

namespace com.DvosTools.bus
{
    public interface IDispatcher
    {
        void Dispatch(Action? action);
    }
}