namespace NEventStore.Persistence.Sql
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    ///     Indicates that a unique constraint on a field violation occurred.
    /// </summary>
    [Serializable]
    public class UniqueConstraintViolationException : Exception
    {
        /// <summary>
        ///     Initializes a new instance of the UniqueConstraintViolationException class.
        /// </summary>
        public UniqueConstraintViolationException()
        {}

        /// <summary>
        ///     Initializes a new instance of the UniqueConstraintViolationException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public UniqueConstraintViolationException(string message)
            : base(message)
        {}

        /// <summary>
        ///     Initializes a new instance of the UniqueConstraintViolationException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The message that is the cause of the current exception.</param>
        public UniqueConstraintViolationException(string message, Exception innerException)
            : base(message, innerException)
        { }

        /// <summary>
        ///     Initializes a new instance of the UniqueConstraintViolationException class.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data of the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected UniqueConstraintViolationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {}
    }
}