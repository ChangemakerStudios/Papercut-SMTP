namespace Papercut.Core.Events
{
    using System.Collections.Generic;
    using System.Linq;

    using Autofac;

    public class AutofacPublishEvent : IPublishEvent
    {
        readonly ILifetimeScope _lifetimeScope;

        public AutofacPublishEvent(ILifetimeScope lifetimeScope)
        {
            _lifetimeScope = lifetimeScope;
        }

        public void Publish<T>(T eventObject) where T : IDomainEvent
        {
            var events = _lifetimeScope.Resolve<IEnumerable<IHandleEvent<T>>>().Distinct().ToList();

            foreach (var @event in events) @event.Handle(eventObject);
        }
    }
}