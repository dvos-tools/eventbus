using System;

namespace com.DvosTools.bus.Dispatchers
{
    public class ImmediateDispatcher : IDispatcher
    {
        public void Dispatch(Action? action)
        {
            action?.Invoke();
        }

        public void DispatchAndWait(Action? action)
        {
            action?.Invoke();
        }
    }
}