using System;

namespace com.DvosTools.bus
{
    /// <summary>
    /// Shared test event classes for unit testing
    /// </summary>
    
    public class TestEvent
    {
        public string Message { get; set; }
        public int Value { get; set; }
    }

    public class AnotherTestEvent
    {
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class RoutableTestEvent : IRoutableEvent
    {
        public Guid AggregateId { get; set; }
        public string Data { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ComplexTestEvent
    {
        public string Name { get; set; }
        public int[] Numbers { get; set; }
        public TestEvent NestedEvent { get; set; }
    }
}