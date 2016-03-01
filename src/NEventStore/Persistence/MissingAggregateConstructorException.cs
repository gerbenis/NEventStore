namespace NEventStore.Persistence
{
    using System;

    public class MissingAggregateConstructorException : Exception
    {
        public MissingAggregateConstructorException(Type type) : base(string.Format("Required constructor not found for the {0} aggregate. Can be private or public constructor with single Guid parameter.", type.FullName))
        {

        }
    }
}
