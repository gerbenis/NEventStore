namespace NEventStore.Persistence.AcceptanceTests
{
    using System;
    using CommonDomain.Core;
    using global::CommonDomain;
    using global::CommonDomain.Core;

    public class UniqueTestAggregate2 : AggregateBase
    {
        public UniqueTestAggregate2(Guid aggregateId, string number, string name) : this()
        {
            RaiseEvent(new UniqueTestAggregateCreated(aggregateId, number, name));
        }

        private UniqueTestAggregate2()
        {
            Register<UniqueTestAggregateCreated>(e =>
            {
                Id = e.AggregateId;
                Name = e.Name;
                Number = e.Number;
            });

            Register<UniqueTestAggregateDeleted>(e =>
            {
                IsDeleted = true;
            });

            Register<UniqueTestAggregateRenamed>(e =>
            {
                Name = e.Name;
            });
        }

        public string Number { get; set; }

        public string Name { get; set; }

        public bool IsDeleted { get; set; }

        public void Delete()
        {
            RaiseEvent(new UniqueTestAggregateDeleted(Id));
        }

        public void ChangeName(string name)
        {
            RaiseEvent(new UniqueTestAggregateRenamed(Id, name));
        }

        protected override IUniqueContraint[] GetUniqueContraints()
        {
            return new IUniqueContraint[]
            {
                new SingleFieldUniqueContraint("Id", Id),
                new SingleFieldUniqueContraint("Name", Name),  
                new SingleFieldUniqueContraint("Number", Number),
                new SingleFieldUniqueContraint("IsDeleted", IsDeleted), 
                new MultiFieldsUniqueConstraint(
                    new SingleFieldUniqueContraint("Name", Name),  
                    new SingleFieldUniqueContraint("IsDeleted", IsDeleted)),
            };
        }
    }
}