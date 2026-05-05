using System;

namespace com.DvosTools.bus
{
    /// <summary>[Obsolete] Use Subscription&lt;T&gt; via the typed handler store.</summary>
    [Obsolete("Subscription is internal post-refactor. Reading EventBusInstance.Handlers returns an empty snapshot.")]
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
