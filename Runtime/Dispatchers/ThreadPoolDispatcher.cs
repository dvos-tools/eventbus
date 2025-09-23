using System;
using System.Threading.Tasks;

namespace com.DvosTools.bus.Runtime.Dispatchers
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
    }
}