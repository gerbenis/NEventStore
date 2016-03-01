namespace NEventStore.Persistence.AcceptanceTests
{
    using System;

    public class UniqueTestAggregateRenamed
    {
        public UniqueTestAggregateRenamed(Guid aggregateId, string name)
        {
            AggregateId = aggregateId;
            Name = name;
        }

        public Guid AggregateId { get; private set; }

        public string Name { get; private set; }
    }
}