// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2022 Jaben Cargman
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


namespace Papercut.Core.Infrastructure.MessageBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Autofac;

    using Papercut.Common.Domain;
    using Papercut.Common.Extensions;

    using Serilog;

    public class AutofacMessageBus : IMessageBus
    {
        #region Fields

        readonly ILifetimeScope _lifetimeScope;

        #endregion

        #region Constructors and Destructors

        public AutofacMessageBus(ILifetimeScope lifetimeScope)
        {
            this._lifetimeScope = lifetimeScope;
        }

        #endregion

        #region Public Methods and Operators

        public virtual async Task<ExecutionResult> ExecuteAsync<T>(T commandObject, CancellationToken token) where T : ICommand
        {
            var commandHandler = this._lifetimeScope.Resolve<ICommandHandler<T>>();

            if (commandHandler != null)
            {
                return await commandHandler.ExecuteAsync(commandObject, token);
            }

            return ExecutionResult.Failure($"No Command Handler for {typeof(T)}");
        }

        public virtual async Task PublishAsync<T>(T eventObject, CancellationToken token) where T : IEvent
        {
            foreach (var @event in this._lifetimeScope.Resolve<IEnumerable<IEventHandler<T>>>().MaybeByOrderable())
            {
                try
                {
                    await this.HandleAsync(eventObject, @event, token);
                }
                catch (Exception ex)
                {
                    this._lifetimeScope.Resolve<ILogger>().ForContext<AutofacMessageBus>().Error(
                        ex,
                        "Failed publishing {EventType} to {EventHandler}",
                        typeof(T),
                        @event.GetType());
                }
            }
        }

        #endregion

        #region Methods

        protected virtual async Task HandleAsync<T>(T eventObject, IEventHandler<T> handler, CancellationToken token)
            where T : IEvent
        {
            await handler.HandleAsync(eventObject, token);
        }

        #endregion
    }
}