namespace NEventStore.Persistence.AcceptanceTests
{
    using System;
    using global::CommonDomain;
    using global::CommonDomain.Core;
    using global::CommonDomain.Persistence;
    using global::CommonDomain.Persistence.EventStore;
    using global::NEventStore;
    using global::NEventStore.Persistence;
    using global::NEventStore.Persistence.Sql.SqlDialects;
    using NEventStore.Persistence.Sql;
    using Xunit;

    public class UniqueContraintTests : IDisposable
    {
        private IStoreEvents eventsStore;

        public UniqueContraintTests()
        {
            eventsStore = WireupEventStore();
        }

        public void Dispose()
        {
            eventsStore.Dispose();
        }

        [Fact]
        public void CanSaveNewAggregateWithUniqueFields()
        {
            var number = DateTime.UtcNow.Ticks.ToString();
            var name = "name" + DateTime.UtcNow.Ticks;
            var aggregateId = SaveUniqueTestAggregate(Guid.NewGuid(), number, name, false);

            using (var repository = CreateRepository())
            {
                var aggregate = repository.GetById<UniqueTestAggregate>(aggregateId);

                Assert.Equal(number, aggregate.Number);
                Assert.Equal(name, aggregate.Name);
                Assert.Equal(false, aggregate.IsDeleted);
            }
        }

        [Fact]
        public void CantSaveTwoAggregatesWithIdenticalName()
        {
            var number = DateTime.UtcNow.Ticks.ToString();
            var name = "name" + DateTime.UtcNow.Ticks;

            SaveUniqueTestAggregate(Guid.NewGuid(), number, name, false);

            Guid secondAggregateId = Guid.NewGuid();

            try
            {
                SaveUniqueTestAggregate(secondAggregateId, number, name, false);

                Assert.True(false, "An exception should be thrown after new an aggregate with the same name is saved.");
            }
            catch (PersistenceException e)
            {
                Assert.Contains("Constraints", e.Message);
            }

            using (var repository = CreateRepository())
            {
                try
                {
                    var agg = repository.GetById<UniqueTestAggregate>(secondAggregateId);
                    Assert.True(agg == null, string.Format("The unique constraints engine failed. Aggregate id={0} was created.", secondAggregateId));
                }
                catch (AggregateNotFoundException)
                {
                }
            }
        }

        [Fact]
        public void CantUpdateAggregateNameToExistingOne()
        {
            var number1 = DateTime.UtcNow.Ticks.ToString();
            var name1 = "name" + DateTime.UtcNow.Ticks;
            var id1 = SaveUniqueTestAggregate(Guid.NewGuid(), number1, name1, false);

            var number2 = DateTime.UtcNow.Ticks.ToString();
            var name2 = "name" + DateTime.UtcNow.Ticks;
            var id2 = SaveUniqueTestAggregate(Guid.NewGuid(), number2, name2, false);

            using (var repository = CreateRepository())
            {
                var agg = repository.GetById<UniqueTestAggregate>(id2);
                agg.ChangeName(name1);

                try
                {
                    repository.Save(agg, Guid.NewGuid());
                    Assert.True(false, "An exception should be thrown after the aggregate #2 is renamed to name #1 and saved.");

                }
                catch (PersistenceException e)
                {
                    Assert.Contains("Constraints", e.Message);
                }
            }
        }

        [Fact]
        public void CanUpdateAggregateNameToExistingOneIfDeleted()
        {
            var number1 = Guid.NewGuid().ToString();
            var name1 = "name" + Guid.NewGuid();
            var id1 = SaveUniqueTestAggregate(Guid.NewGuid(), number1, name1, true);

            var number2 = Guid.NewGuid().ToString();
            var name2 = "name" + Guid.NewGuid();
            var id2 = SaveUniqueTestAggregate(Guid.NewGuid(), number2, name2, false);

            using (var repository = CreateRepository())
            {
                var agg2 = repository.GetById<UniqueTestAggregate>(id2);
                agg2.ChangeName(name1);
                repository.Save(agg2, Guid.NewGuid());
            }

            using (var repository = CreateRepository())
            {
                var agg2 = repository.GetById<UniqueTestAggregate>(id2);

                Assert.Equal(name1, agg2.Name);
            }
        }

        [Fact]
        public void ShouldSaveAggregateWithWith6UC()
        {
            using (var repository = CreateRepository())
            {
                UniqueTestAggregate2 agg = new UniqueTestAggregate2(Guid.NewGuid(), "test1", "name1");

                Assert.Throws<PersistenceException>(() =>
                {
                    repository.Save(agg, Guid.NewGuid());
                });
            }
        }

        private Guid SaveUniqueTestAggregate(Guid id, string number, string name, bool isDeleted, IRepository repositoryToUse = null)
        {
            IRepository repository = null;

            try
            {
                repository = repositoryToUse ?? CreateRepository();

                var aggregateToSave = new UniqueTestAggregate(id, number, name);

                if (isDeleted)
                {
                    aggregateToSave.Delete();
                }

                repository.Save(aggregateToSave, Guid.NewGuid(), d => { });

                return aggregateToSave.Id;
            }
            finally
            {
                if (repository != null && repositoryToUse == null)
                {
                    repository.Dispose();
                }
            }
        }

        private IStoreEvents WireupEventStore()
        {
            return Wireup.Init()
               .LogToOutputWindow()
               .UsingInMemoryPersistence()
               .UsingSqlPersistence("NEventStore") // Connection string is in app.config
                   .WithDialect(new MsSqlDialect())
                   .InitializeStorageEngine()
                   .EnlistInAmbientTransaction() // two-phase commit
                   .TrackPerformanceInstance("example")
                   .UsingJsonSerialization()
               //  .Compress()
               //   .EncryptWith(EncryptionKey)
               //.HookIntoPipelineUsing(new[] { new NEventStorePipelineHook(() => busControl) })
               .Build();
        }

        private IRepository CreateRepository()
        {
            return new EventStoreRepository(eventsStore, new AggregateFacory(), new ConflictDetector(), new EmptyEventTypeBridge());
        }
    }
}