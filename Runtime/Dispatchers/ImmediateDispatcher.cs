using System;

namespace com.DvosTools.bus.Runtime.Dispatchers
{
    public class ImmediateDispatcher : IDispatcher
    {
        public void Dispatch(Action? action)
        {
            action?.Invoke();
        }
    }
}