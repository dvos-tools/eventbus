using System;

namespace com.DvosTools.bus
{
    public class Subscription
    {
        public Action<object> Handler { get; set; }
        public IDispatcher Dispatcher { get; set; }
        public Guid AggregateId { get; set; }

        public Subscription(Action<object> handler, IDispatcher dispatcher, Guid aggregateId = default)
        {
            Handler = handler;
            Dispatcher = dispatcher;
            AggregateId = aggregateId;
        }
    }
}