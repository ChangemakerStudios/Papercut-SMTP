// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.


using Papercut.Core.Infrastructure.MessageBus;

namespace Papercut.AppLayer.Events;

public class EventPublishAll : AutofacMessageBus
{
    private readonly IEventAggregator _eventAggregator;

    public EventPublishAll(
        ILifetimeScope scope,
        IEventAggregator eventAggregator)
        : base(scope)
    {
        this._eventAggregator = eventAggregator;
    }

    public override async Task PublishAsync<T>(T eventObject, CancellationToken token)
    {
        if (eventObject == null) throw new ArgumentNullException(nameof(eventObject));

        await base.PublishAsync(eventObject, token);
        await this._eventAggregator.PublishOnUIThreadAsync(eventObject, token);
    }

    protected override async Task HandleAsync<T>(T eventObject, IEventHandler<T> @event, CancellationToken token)
    {
        await Execute.OnUIThreadAsync(async () => await base.HandleAsync(eventObject, @event, token));
    }

    #region Begin Static Container Registrations

    /// <summary>
    /// Called dynamically from the RegisterStaticMethods() call in the container module.
    /// </summary>
    /// <param name="builder"></param>
    [UsedImplicitly]
    static void Register(ContainerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RegisterType<EventPublishAll>().As<IMessageBus>().InstancePerLifetimeScope();
    }

    #endregion
}