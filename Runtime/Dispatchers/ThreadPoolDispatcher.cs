using System;
using System.Threading.Tasks;

namespace com.DvosTools.bus.Dispatchers
{
    public class ThreadPoolDispatcher : IDispatcher
    {
        public void Dispatch(Action? action)
        {
            if (action != null)
            {
                Task.Run(action);
            }
        }

        public void DispatchAndWait(Action? action)
        {
            if (action != null)
            {
                Task.Run(action).Wait();
            }
        }
    }
}