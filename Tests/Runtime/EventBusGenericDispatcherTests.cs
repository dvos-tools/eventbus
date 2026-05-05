#nullable enable
using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace com.DvosTools.bus
{
    [TestFixture]
    public class EventBusGenericDispatcherTests
    {
        [SetUp] public void SetUp() => EventBus.ClearAll();
        [TearDown] public void TearDown() => EventBus.ClearAll();

        private sealed class CapturingDispatcher : IDispatcher
        {
            public Type? CapturedT;
            public object? CapturedState;

            public void Dispatch<T>(Action<T> handler, in T state, string? eventTypeName = null, Guid? aggregateId = null)
                => Capture<T>(handler, state);

            public void DispatchAndWait<T>(Action<T> handler, in T state, string? eventTypeName = null, Guid? aggregateId = null)
                => Capture<T>(handler, state);

            public Task DispatchAndWaitAsync<T>(Action<T> handler, in T state, string? eventTypeName = null, Guid? aggregateId = null)
            {
                Capture<T>(handler, state);
                return Task.CompletedTask;
            }

            private void Capture<T>(Action<T> handler, T state)
            {
                CapturedT = typeof(T);
                CapturedState = state;
                handler(state);
            }
        }

        [Test]
        public void Dispatcher_ReceivesTypedT_NotObject()
        {
            var d = new CapturingDispatcher();
            EventBus.RegisterHandler<StructTestEvent>(e => { /* noop */ }, Guid.Empty, d);

            EventBus.SendAndWait(new StructTestEvent { Value = 5 });

            Assert.AreEqual(typeof(StructTestEvent), d.CapturedT);
            Assert.IsInstanceOf<StructTestEvent>(d.CapturedState);
            Assert.AreEqual(5, ((StructTestEvent)d.CapturedState!).Value);
        }
    }
}
