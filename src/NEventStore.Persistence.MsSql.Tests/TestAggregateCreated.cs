namespace NEventStore.Persistence.AcceptanceTests
{
    using System;

    public class TestAggregateCreated
    {
        public TestAggregateCreated(Guid aggregateId)
        {
            AggregateId = aggregateId;
        }

        public Guid AggregateId { get; private set; }
    }
}