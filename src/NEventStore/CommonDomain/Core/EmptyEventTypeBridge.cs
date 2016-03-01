namespace CommonDomain.Core
{
    using CommonDomain;

    public class EmptyEventTypeBridge : IEventTypeBridge
    {
        public object GetEvent(object @event, string eventName)
        {
            return null;
        }

        public string ResolveEventName(object @event)
        {
            return null;
        }
    }
}
