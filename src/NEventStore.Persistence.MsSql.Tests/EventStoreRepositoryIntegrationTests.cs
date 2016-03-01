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
    using Xunit;

    public class EventStoreRepositoryIntegrationTests : IDisposable
    {
        private IStoreEvents eventsStore;

        public EventStoreRepositoryIntegrationTests()
        {
            eventsStore = WireupEventStore();
        }

        public void Dispose()
        {
            eventsStore.Dispose();
        }

        [Fact]
        public void CanGetLatestVersionById()
        {
            var savedId = SaveTestAggregate(3000);

            using (var repository = CreateRepository())
            {
                var retrieved = repository.GetById<TestAggregate>(savedId);
                Assert.Equal(3000, retrieved.AppliedEventCount);
            }
        }

        [Fact]
        public void CanGetSpecificVersionFromFirstPageById()
        {
            var savedId = SaveTestAggregate(100);

            using (var repository = CreateRepository())
            {
                var retrieved = repository.GetById<TestAggregate>(savedId, 65);
                Assert.Equal(64, retrieved.AppliedEventCount);
            }
        }

        [Fact]
        public void CanGetSpecificVersionFromSubsequentPageById()
        {
            var savedId = SaveTestAggregate(500);

            using (var repository = CreateRepository())
            {
                var retrieved = repository.GetById<TestAggregate>(savedId, 126);
                Assert.Equal(125, retrieved.AppliedEventCount);
            }
        }

        [Fact]
        public void CanGetSpecificVersionFromFirstPageByIdUsingSingleRepository()
        {
            using (var repository = CreateRepository())
            {
                var savedId = SaveTestAggregate(100, repository);
                var retrieved = repository.GetById<TestAggregate>(savedId, 65);
                Assert.Equal(64, retrieved.AppliedEventCount);
            }
        }

        [Fact]
        public void CanGetSpecificVersionFromSubsequentPageByIddUsingSingleRepository()
        {
            using (var repository = CreateRepository())
            {
                var savedId = SaveTestAggregate(500, repository);

                var retrieved = repository.GetById<TestAggregate>(savedId, 126);
                Assert.Equal(125, retrieved.AppliedEventCount);
            }
        }

        [Fact]
        public void CanHandleLargeNumberOfEventsInOneTransaction()
        {
            const int numberOfEvents = 50000;

            var aggregateId = SaveTestAggregate(numberOfEvents);

            using (var repository = CreateRepository())
            {
                var saved = repository.GetById<TestAggregate>(aggregateId);
                Assert.Equal(numberOfEvents, saved.AppliedEventCount);
            }
        }

        [Fact]
        public void CanSaveExistingAggregate()
        {
            var savedId = SaveTestAggregate(100);

            using (var repository = CreateRepository())
            {
                var firstSaved = repository.GetById<TestAggregate>(savedId);
                firstSaved.ProduceEvents(50);
                repository.Save(firstSaved, Guid.NewGuid(), d => { });

                var secondSaved = repository.GetById<TestAggregate>(savedId);
                Assert.Equal(150, secondSaved.AppliedEventCount);
            }
        }

        [Fact]
        public void CanSaveMultiplesOfWritePageSize()
        {
            var savedId = SaveTestAggregate(1500);

            using (var repository = CreateRepository())
            {
                var saved = repository.GetById<TestAggregate>(savedId);

                Assert.Equal(1500, saved.AppliedEventCount);
            }
        }

        [Fact]
        public void ClearsEventsFromAggregateOnceCommitted()
        {
            var aggregateToSave = new TestAggregate(Guid.NewGuid());

            using (var repository = CreateRepository())
            {
                aggregateToSave.ProduceEvents(10);
                repository.Save(aggregateToSave, Guid.NewGuid(), d => { });

                Assert.Equal(0, ((IAggregate) aggregateToSave).GetUncommittedEvents().Count);
            }
        }

        [Fact]
        public void GetsEventsFromCorrectStreams()
        {
            var aggregate1Id = SaveTestAggregate(100);
            var aggregate2Id = SaveTestAggregate(50);
            using (var repository = CreateRepository())
            {
                var firstSaved = repository.GetById<TestAggregate>(aggregate1Id);
                Assert.Equal(100, firstSaved.AppliedEventCount);

                var secondSaved = repository.GetById<TestAggregate>(aggregate2Id);
                Assert.Equal(50, secondSaved.AppliedEventCount);
            }
        }

        [Fact]
        public void ThrowsOnGetNonExistentAggregate()
        {
            using (var repository = CreateRepository())
            {
                Assert.Throws<AggregateNotFoundException>(() => repository.GetById<TestAggregate>(Guid.NewGuid()));
            }
        }

        private Guid SaveTestAggregate(int numberOfEvents, IRepository repositoryToUse = null)
        {
            IRepository repository = null;
            try
            {
                repository = repositoryToUse ?? CreateRepository();

                var aggregateToSave = new TestAggregate(Guid.NewGuid());
                aggregateToSave.ProduceEvents(numberOfEvents);
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