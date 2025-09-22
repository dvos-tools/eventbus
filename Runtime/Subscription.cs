using System;

namespace com.DvosTools.bus.Runtime
{
    public class Subscription
    {
        public Action<object> Handler { get; set; }
        public bool RequiresMainThread { get; set; }

        public Subscription(Action<object> handler, bool requiresMainThread)
        {
            Handler = handler;
            RequiresMainThread = requiresMainThread;
        }
    }
}