namespace NEventStore.Persistence.AcceptanceTests
{
    using System;

    public class UniqueTestAggregateCreated
    {
        public UniqueTestAggregateCreated(Guid aggregateId, string number, string name)
        {
            AggregateId = aggregateId;
            Name = name;
            Number = number;
        }

        public Guid AggregateId { get; private set; }

        public string Name { get; private set; }

        public string Number { get; private set; }
    }
}