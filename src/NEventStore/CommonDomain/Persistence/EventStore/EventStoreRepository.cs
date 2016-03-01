using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using CommonDomain;
using CommonDomain.Persistence;
using NEventStore;
using NEventStore.Persistence;
using System.Reflection;
using System.Transactions;
using System.Transactions;

namespace CommonDomain.Persistence.EventStore
{
    using Newtonsoft.Json;

    public class EventStoreRepository : IRepository, IDisposable
     {
        private const string AggregateTypeHeader = "aggregate";

        private const string EventNameHeader = "eventType";

        private readonly IDetectConflicts _conflictDetector;

        private readonly IStoreEvents _eventStore;

        private readonly IConstructAggregates _factory;

         private readonly IEventTypeBridge _eventTypeBridge;

        private readonly IDictionary<string, ISnapshot> _snapshots = new Dictionary<string, ISnapshot>();

        private readonly IDictionary<string, IEventStream> _streams = new Dictionary<string, IEventStream>();

        public EventStoreRepository(IStoreEvents eventStore, IConstructAggregates factory, IDetectConflicts conflictDetector, IEventTypeBridge eventTypeBridge)
        {
            _eventStore = eventStore;
            _factory = factory;
            _conflictDetector = conflictDetector;
            _eventTypeBridge = eventTypeBridge;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual TAggregate GetById<TAggregate>(Guid id) where TAggregate : class, IAggregate
        {
            return GetById<TAggregate>(Bucket.Default, id);
        }

        public virtual TAggregate GetById<TAggregate>(Guid id, int versionToLoad) where TAggregate : class, IAggregate
        {
            return GetById<TAggregate>(Bucket.Default, id, versionToLoad);
        }

        public TAggregate GetById<TAggregate>(string bucketId, Guid id) where TAggregate : class, IAggregate
        {
            return GetById<TAggregate>(bucketId, id, int.MaxValue);
        }

        public TAggregate GetById<TAggregate>(string bucketId, Guid id, int versionToLoad) where TAggregate : class, IAggregate
        {
            if (versionToLoad <= 0)
            {
                throw new InvalidOperationException("Cannot get version <= 0");
            }

            ISnapshot snapshot = GetSnapshot(bucketId, id, versionToLoad);
            IEventStream stream = OpenStream(bucketId, id, versionToLoad, snapshot);
            IAggregate aggregate = GetAggregate<TAggregate>(snapshot, stream);

            if (stream.CommittedEvents.Count == 0 && stream.UncommittedEvents.Count == 0 || aggregate == null)
            {
                throw new AggregateNotFoundException(id, typeof(TAggregate));
            }

            ApplyEventsToAggregate(versionToLoad, stream, aggregate);

            return aggregate as TAggregate;
        }

        public virtual void Save(IAggregate aggregate, Guid commitId, Action<IDictionary<string, object>> updateHeaders)
        {
            Save(Bucket.Default, aggregate, commitId, updateHeaders);
        }

        public void Save(string bucketId, IAggregate aggregate, Guid commitId, Action<IDictionary<string, object>> updateHeaders)
        {
            Dictionary<string, object> headers = PrepareHeaders(aggregate, updateHeaders);
            while (true)
            {
                IEventStream stream = PrepareStream(bucketId, aggregate, headers);
                int commitEventCount = stream.CommittedEvents.Count;

                try
                {
                    stream.CommitChanges(commitId, aggregate.GetUniqueContraints());
                    aggregate.ClearUncommittedEvents();

                    return;
                }

                catch (DuplicateCommitException)
                {
                    stream.ClearChanges();
                    return;
                }
                catch (ConcurrencyException e)
                {
                    var conflict = ThrowOnConflict(stream, commitEventCount);
                    stream.ClearChanges();

                    if (conflict)
                    {
                        throw new ConflictingCommandException(e.Message, e);
                    }
                }
                catch (StorageException e)
                {
                    throw new PersistenceException(e.Message, e);
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            lock (_streams)
            {
                foreach (var stream in _streams)
                {
                    stream.Value.Dispose();
                }

                _snapshots.Clear();
                _streams.Clear();
            }
        }

        private void ApplyEventsToAggregate(int versionToLoad, IEventStream stream, IAggregate aggregate)
        {
            if (versionToLoad == 0 || aggregate.Version < versionToLoad)
            {
                var i = 1;
                foreach (var @event in stream.CommittedEvents)
                {
                    var headers = @event.Headers;
                    object e;

                    if (headers.ContainsKey(EventNameHeader))
                    {
                        var eventName = headers[EventNameHeader];
                        e = _eventTypeBridge.GetEvent(@event.Body, eventName.ToString());
                    }
                    else
                    {
                        e = @event.Body;
                    }

                    aggregate.ApplyEvent(e);

                    if (versionToLoad == i++)
                    {
                        break;
                    }
                }
            }
        }

         private IAggregate GetAggregate<TAggregate>(ISnapshot snapshot, IEventStream stream)
        {
            IMemento memento = snapshot == null ? null : snapshot.Payload as IMemento;
            return _factory.Build(typeof(TAggregate), new Guid(stream.StreamId), memento);
        }

        private ISnapshot GetSnapshot(string bucketId, Guid id, int version)
        {
            ISnapshot snapshot;
            var snapshotId = bucketId + id;
            if (!_snapshots.TryGetValue(snapshotId, out snapshot))
            {
                _snapshots[snapshotId] = snapshot = _eventStore.Advanced.GetSnapshot(bucketId, id, version);
            }

            return snapshot;
        }

        private IEventStream OpenStream(string bucketId, Guid id, int version, ISnapshot snapshot)
        {
            IEventStream stream;
            var streamId = bucketId + "+" + id;
            if (_streams.TryGetValue(streamId, out stream))
            {
                return stream;
            }

            stream = snapshot == null
                ? _eventStore.OpenStream(bucketId, id, 0, version)
                : _eventStore.OpenStream(snapshot, version);

            return _streams[streamId] = stream;
        }

        private IEventStream PrepareStream(string bucketId, IAggregate aggregate, Dictionary<string, object> headers)
        {
            IEventStream stream;
            var streamId = bucketId + "+" + aggregate.Id;
            if (!_streams.TryGetValue(streamId, out stream))
            {
                _streams[streamId] = stream = _eventStore.CreateStream(bucketId, aggregate.Id);
            }

            foreach (var item in headers)
            {
                stream.UncommittedHeaders[item.Key] = item.Value;
            }

            var uncommittedEvents = aggregate.GetUncommittedEvents();
            foreach (var uncommittedEvent in uncommittedEvents)
            {
                var eventMessage = new EventMessage();
              
                var eventTypeName = _eventTypeBridge.ResolveEventName(uncommittedEvent);
                if (eventTypeName != null)
                {
                    eventMessage.Headers = new Dictionary<string, object> {{EventNameHeader, eventTypeName}};
                }

                eventMessage.Body = uncommittedEvent;
                stream.Add(eventMessage);
            }

            return stream;
        }

        private static Dictionary<string, object> PrepareHeaders(
            IAggregate aggregate, Action<IDictionary<string, object>> updateHeaders)
        {
            var headers = new Dictionary<string, object>();

            headers[AggregateTypeHeader] = aggregate.GetType().Name;
            if (updateHeaders != null)
            {
                updateHeaders(headers);
            }

            return headers;
        }

        private bool ThrowOnConflict(IEventStream stream, int skip)
        {
            IEnumerable<object> committed = stream.CommittedEvents.Skip(skip).Select(x => x.Body);
            IEnumerable<object> uncommitted = stream.UncommittedEvents.Select(x => x.Body);
            return _conflictDetector.ConflictsWith(uncommitted, committed);
        }
    }
}
