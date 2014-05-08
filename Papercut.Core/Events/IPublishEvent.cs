namespace Papercut.Core.Events
{
    public interface IPublishEvent
    {
        void Publish<T>(T eventObject) where T : IDomainEvent;
    }
}