using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace com.DvosTools.bus
{
    public struct StructTestEvent
    {
        public int Value;
        public float Time;
    }

    public struct RoutableStructEvent
    {
        public Guid AggregateId;
        public string Tag;
    }

    [TestFixture]
    public class EventBusStructEventTests
    {
        [SetUp] public void SetUp() => EventBus.ClearAll();
        [TearDown] public void TearDown() => EventBus.ClearAll();

        [Test]
        public void SendAndWait_Struct_HandlerReceivesEvent()
        {
            int seen = 0;
            EventBus.RegisterHandler<StructTestEvent>(e => seen = e.Value, Guid.Empty, new TestSyncDispatcher());

            EventBus.SendAndWait(new StructTestEvent { Value = 42, Time = 1.5f });

            Assert.AreEqual(42, seen);
        }

        [Test]
        public void SendAndWait_StructRouted_OnlyMatchingAggregateReceives()
        {
            var idA = Guid.NewGuid();
            var idB = Guid.NewGuid();
            int seenA = 0, seenB = 0;
            EventBus.RegisterHandler<StructTestEvent>(e => seenA = e.Value, idA, new TestSyncDispatcher());
            EventBus.RegisterHandler<StructTestEvent>(e => seenB = e.Value, idB, new TestSyncDispatcher());

            EventBus.SendAndWait(new StructTestEvent { Value = 7 }, aggregateId: idA);

            Assert.AreEqual(7, seenA);
            Assert.AreEqual(0, seenB);
        }

        [UnityTest]
        public IEnumerator Send_StructEvent_QueueDeliversToHandler()
        {
            int seen = 0;
            EventBus.RegisterHandler<StructTestEvent>(e => seen = e.Value, Guid.Empty, new TestSyncDispatcher());

            EventBus.Send(new StructTestEvent { Value = 99 });
            for (int i = 0; i < 10 && seen == 0; i++) yield return null;

            Assert.AreEqual(99, seen);
        }
    }
}
