namespace NEventStore.Persistence.AcceptanceTests
{
    using System;
    using CommonDomain.Core;
    using global::CommonDomain.Core;

    public class TestAggregate : AggregateBase
    {
        public TestAggregate(Guid aggregateId) : this()
        {
            RaiseEvent(new TestAggregateCreated(aggregateId));
        }

        private TestAggregate()
        {
            Register<TestAggregateCreated>(e => Id = e.AggregateId);
            Register<WoftamEvent>(e => AppliedEventCount++);
        }

        public int AppliedEventCount { get; private set; }

        public void ProduceEvents(int count, string property1Prefix = null)
        {
            for (int i = 0; i < count; i++)
            {
                RaiseEvent(new WoftamEvent(property1Prefix ?? string.Empty + "Woftam1-" + i, "Woftam2-" + i));
            }
        }
    }
}