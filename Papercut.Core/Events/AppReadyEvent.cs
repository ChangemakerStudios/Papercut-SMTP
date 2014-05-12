namespace Papercut.Core.Events
{
    public class AppPreStartEvent : IDomainEvent
    {
        public bool CancelStart { get; set; }
        public AppPreStartEvent(bool cancelStart = false)
        {
            CancelStart = cancelStart;
        }
    }

    public class AppReadyEvent : IDomainEvent
    {
         
    }
}