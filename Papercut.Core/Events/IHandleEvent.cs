namespace Papercut.Core.Events
{
    public interface IHandleEvent<in TEvent>
        where TEvent : IDomainEvent
    {
        void Handle(TEvent @event);
    }
}