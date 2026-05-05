#nullable enable
using System;

namespace com.DvosTools.bus.Core
{
    internal sealed class Subscription<T>
    {
        public readonly Action<T> Handler;
        public readonly IDispatcher Dispatcher;
        public readonly Guid AggregateId;
        public readonly Delegate OriginalHandler;

        public Subscription(Action<T> handler, IDispatcher dispatcher, Guid aggregateId, Delegate originalHandler)
        {
            Handler = handler;
            Dispatcher = dispatcher;
            AggregateId = aggregateId;
            OriginalHandler = originalHandler;
        }
    }
}
