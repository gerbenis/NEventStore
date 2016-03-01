using System;
using System.Text;
using global::CommonDomain;

namespace NEventStore.CommonDomain.Core
{
    using System.Linq;

    public class MultiFieldsUniqueConstraint : IUniqueContraint
    {
        public string UniquePayload { get; }

        public string UniqueConstraintName { get; }

        public MultiFieldsUniqueConstraint(params IUniqueContraint[] uniqueContraints)
        {
            UniqueConstraintName = string.Join("-", uniqueContraints.Select(f => f.UniqueConstraintName));

            StringBuilder payload = new StringBuilder();
            foreach (var contraint in uniqueContraints)
            {
                payload.Append("{");
                payload.Append(contraint.UniquePayload);
                payload.Append("}");
            }

            UniquePayload = payload.ToString();
        }
    }

    public class SingleFieldUniqueContraint : IUniqueContraint
    {
        public string UniquePayload { get; }

        public string UniqueConstraintName { get; }

        public SingleFieldUniqueContraint(string fieldName, object value)
        {
            UniqueConstraintName = fieldName.ToLowerInvariant();

            StringBuilder payload = new StringBuilder();
            if (value != null)
            {
                if (value is string)
                {
                   payload.Append(((string)value).ToLowerInvariant().GetHashCode());
                }
                else if (value is DateTime)
                {
                    payload.Append(((DateTime) value).Ticks);
                }
                else if (value is TimeSpan)
                {
                    payload.Append(((TimeSpan) value).Ticks);
                }
                else
                {
                    payload.Append(value.ToString().ToLowerInvariant());
                }
            }
            else
            {
                payload.Append("{null}");
            }

            UniquePayload = payload.ToString();
        }
    }
}
