using System;
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using com.DvosTools.bus.Dispatchers;

namespace com.DvosTools.bus
{
    public class DrainBudgetEvent { public int N; }

    [TestFixture]
    public class EventBusDrainBudgetTests
    {
        private int _originalBudget;

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _originalBudget = UnityDispatcher.DrainBudgetPerFrame;
        }

        [TearDown]
        public void TearDown()
        {
            UnityDispatcher.DrainBudgetPerFrame = _originalBudget;
            EventBus.ClearAll();
        }

        [UnityTest]
        public IEnumerator DrainBudget_Five_ThirtyJobsRequireSixFrames()
        {
            UnityDispatcher.DrainBudgetPerFrame = 5;
            int count = 0;
            // Force cross-thread path: handler registered with UnityDispatcher; bus worker is a background thread.
            EventBus.RegisterHandler<DrainBudgetEvent>(_ => count++, Guid.Empty, UnityDispatcher.Instance);

            // Send from background thread so dispatch enters cross-thread queue.
            var task = Task.Run(() =>
            {
                for (int i = 0; i < 30; i++)
                    EventBus.Send(new DrainBudgetEvent { N = i });
            });
            while (!task.IsCompleted) yield return null;

            int frames = 0;
            while (count < 30 && frames < 100)
            {
                yield return null;
                frames++;
            }

            Assert.AreEqual(30, count);
            Assert.GreaterOrEqual(frames, 6, $"With budget=5 and 30 jobs, expected at least 6 frames; got {frames}");
        }
    }
}
