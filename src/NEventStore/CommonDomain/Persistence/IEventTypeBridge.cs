namespace CommonDomain
{
    public interface IEventTypeBridge
    {
        object GetEvent(object @event, string eventTypeName);

        string ResolveEventName(object @event);
    }
}
