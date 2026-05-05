using System;
using NUnit.Framework;
using com.DvosTools.bus.Dispatchers;

namespace com.DvosTools.bus
{
    [TestFixture]
    public class EventBusZeroAllocTests
    {
        [SetUp] public void SetUp() => EventBus.ClearAll();
        [TearDown] public void TearDown() => EventBus.ClearAll();

        [Test]
        public void SendAndWait_StructEvent_MainThreadUnityDispatcher_ZeroAllocAfterWarmup()
        {
            int sum = 0;
            // UnityDispatcher.Instance triggers GameObject creation; do that out of the measured region.
            var dispatcher = UnityDispatcher.Instance!;
            EventBus.RegisterHandler<StructTestEvent>(e => sum += e.Value, Guid.Empty, dispatcher);

            // Warm up: JIT, snapshots, registry, etc.
            for (int i = 0; i < 100; i++)
                EventBus.SendAndWait(new StructTestEvent { Value = 1 });

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
                EventBus.SendAndWait(new StructTestEvent { Value = 1 });
            long after = GC.GetAllocatedBytesForCurrentThread();

            long delta = after - before;
            // Allow tiny bookkeeping noise from Unity logging if any sneaks through; expect 0 in release.
            Assert.LessOrEqual(delta, 0, $"Hot path allocated {delta} bytes over 1000 sends. Target: 0.");
            Assert.Greater(sum, 0); // sanity
        }
    }
}
