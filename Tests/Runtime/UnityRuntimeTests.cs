using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace com.DvosTools.bus
{
    /// <summary>
    /// Legacy test file - tests have been moved to dedicated files:
    /// - EventBusBasicRegistrationTests.cs
    /// - EventBusUnityRuntimeTests.cs  
    /// - EventBusAggregateIdTests.cs
    /// - TestEvents.cs
    /// </summary>
    public class UnityRuntimeTests
    {
        // A Test behaves as an ordinary method
        [Test]
        public void NewTestScriptSimplePasses()
        {
            // Use the Assert class to test conditions
            Assert.IsTrue(true);
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator NewTestScriptWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
            Assert.IsTrue(true);
        }
    }
}