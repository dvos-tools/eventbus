using System;
using com.DvosTools.bus.Runtime.Dispatchers;

namespace com.DvosTools.bus.Runtime
{
    public class Subscription
    {
        public Action<object> Handler { get; set; }
        public IDispatcher Dispatcher { get; set; }

        public Subscription(Action<object> handler, IDispatcher dispatcher)
        {
            Handler = handler;
            Dispatcher = dispatcher;
        }
    }
}