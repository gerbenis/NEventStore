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

    public class ConcurrencyCheckTests : IDisposable
    {
        private IStoreEvents eventsStore;

        public ConcurrencyCheckTests()
        {
            eventsStore = WireupEventStore();
        }

        public void Dispose()
        {
            eventsStore.Dispose();
        }

        [Fact]
        public void CanTrackVersionsWithSingleRepository()
        {
            var number = DateTime.UtcNow.Ticks.ToString();
            var name = "name" + DateTime.UtcNow.Ticks;
            var original  = CreateTestAggregate(number, name, false);

            UniqueTestAggregate agg1, agg2, agg3;

            using (var repository = CreateRepository())
            {
                agg1 = repository.GetById<UniqueTestAggregate>(original.Id);

                Assert.NotNull(agg1);
                Assert.Equal(number, agg1.Number);
                Assert.Equal(name, agg1.Name);
                Assert.Equal(false, agg1.IsDeleted);
            }

            using (var repository = CreateRepository())
            {
                agg2 = repository.GetById<UniqueTestAggregate>(original.Id);
                agg2.ChangeName("#2" + name);
                SaveTestAggregate(agg2, repository);

                Assert.Equal(number, agg1.Number);
                Assert.Equal("#2" + name, agg2.Name);
                Assert.Equal(false, agg1.IsDeleted);
            }

            using (var repository = CreateRepository())
            {
                Assert.Equal(name, agg1.Name);
                agg1.ChangeName("#3" + name);

                // It should fail as aggregate updated to #2 name and the agg1 has older version.
                Assert.Throws<ConflictingCommandException>(() =>
                {
                    SaveTestAggregate(agg1, repository);
                });

                agg3 = repository.GetById<UniqueTestAggregate>(original.Id);
                Assert.Equal(agg2.Name, agg3.Name);
            }
        }


        private void SaveTestAggregate(UniqueTestAggregate aggregate, IRepository repositoryToUse = null)
        {
            IRepository repository = null;

            try
            {
                repository = repositoryToUse ?? CreateRepository();

                repository.Save(aggregate, Guid.NewGuid(), d => { });
            }
            finally
            {
                if (repository != null && repositoryToUse == null)
                {
                    repository.Dispose();
                }
            }
        }

        private UniqueTestAggregate CreateTestAggregate(string number, string name, bool isDeleted, IRepository repositoryToUse = null)
        {
            IRepository repository = null;

            try
            {
                repository = repositoryToUse ?? CreateRepository();

                var aggregateToSave = new UniqueTestAggregate(Guid.NewGuid(), number, name);

                if (isDeleted)
                {
                    aggregateToSave.Delete();
                }

                repository.Save(aggregateToSave, Guid.NewGuid(), d => { });

                return aggregateToSave;
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