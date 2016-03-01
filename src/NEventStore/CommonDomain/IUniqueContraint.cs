namespace CommonDomain
{
    public interface IUniqueContraint
    {
        string UniqueConstraintName { get; }

        string UniquePayload { get; }
    }
}
