namespace NEventStore.Persistence
{
    using System;
    using System.Reflection;
    using CommonDomain;
    using global::CommonDomain;
    using global::CommonDomain.Persistence;

    public class AggregateFacory : IConstructAggregates
    {
        public IAggregate Build(Type type, Guid id, IMemento snapshot)
        {
            // Try to find s constructor with one Guid parameter.
            var constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(Guid) }, null);
            if (constructor != null)
            {
                return constructor.Invoke(new object[] { id }) as IAggregate;
            }

            // Try to find parameterless constructur
            constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            if (constructor != null)
            {
                var aggregate = constructor.Invoke(null) as IAggregate;
                SetPrivatePropertyValue(aggregate, "Id", id);

                return aggregate;
            }

            // TODO: add snapshots support!

            throw new MissingAggregateConstructorException(type);
        }

        private void SetPrivatePropertyValue<T>(object obj, string propName, T val)
        {
            Type t = obj.GetType();
            if (t.GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) == null)
            {
                throw new ArgumentOutOfRangeException("propName", string.Format("Property {0} was not found in Type {1}", propName, obj.GetType().FullName));
            }

            t.InvokeMember(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance, null, obj, new object[] { val });
        }
    }
}
