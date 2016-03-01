namespace NEventStore.Persistence.AcceptanceTests
{
    using System;

    public class UniqueTestAggregateDeleted
    {
        public UniqueTestAggregateDeleted(Guid aggregateId)
        {
            AggregateId = aggregateId;
        }

        public Guid AggregateId { get; private set; }
    }
}