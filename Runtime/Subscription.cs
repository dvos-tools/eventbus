using System;

namespace com.DvosTools.bus
{
    public class Subscription
    {
        public Action<object> Handler { get; set; }
        public IDispatcher Dispatcher { get; set; }
        public Guid AggregateId { get; set; }
        public object OriginalHandler { get; set; }

        public Subscription(Action<object> handler, IDispatcher dispatcher, Guid aggregateId, object originalHandler)
        {
            Handler = handler;
            Dispatcher = dispatcher;
            AggregateId = aggregateId;
            OriginalHandler = originalHandler;
        }
    }
}