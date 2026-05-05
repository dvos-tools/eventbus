using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace com.DvosTools.bus
{
    public class FifoEventA { public int Index; }
    public class FifoEventB { public int Index; }

    [TestFixture]
    public class EventBusFifoOrderTests
    {
        [SetUp] public void SetUp() => EventBus.ClearAll();
        [TearDown] public void TearDown() => EventBus.ClearAll();

        [UnityTest]
        public IEnumerator Send_AlternatingTypes_CrossTypeFifoPreserved()
        {
            var arrivals = new List<string>();
            EventBus.RegisterHandler<FifoEventA>(e => arrivals.Add($"A{e.Index}"), Guid.Empty, new TestSyncDispatcher());
            EventBus.RegisterHandler<FifoEventB>(e => arrivals.Add($"B{e.Index}"), Guid.Empty, new TestSyncDispatcher());

            for (int i = 0; i < 10; i++)
            {
                EventBus.Send(new FifoEventA { Index = i });
                EventBus.Send(new FifoEventB { Index = i });
            }

            for (int frame = 0; frame < 60 && arrivals.Count < 20; frame++) yield return null;

            Assert.AreEqual(20, arrivals.Count, "queue did not drain");
            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual($"A{i}", arrivals[i * 2]);
                Assert.AreEqual($"B{i}", arrivals[i * 2 + 1]);
            }
        }
    }
}
