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


using Autofac;

using Papercut.Common.Domain;
using Papercut.Core.Domain.Message;

namespace Papercut.Service.TrayNotification;

/// <summary>
/// Handles new message notifications and displays balloon tips
/// </summary>
public class NewMessageNotificationService(ILogger logger) : IEventHandler<NewMessageEvent>
{
    private bool _notificationsEnabled = true;

    public event EventHandler<NewMessageEvent>? NewMessageReceived;

    public bool NotificationsEnabled
    {
        get => _notificationsEnabled;
        set => _notificationsEnabled = value;
    }

    public Task HandleAsync(NewMessageEvent @event, CancellationToken token = default)
    {
        if (!_notificationsEnabled)
        {
            logger.Debug("Notifications disabled, skipping notification for message");
            return Task.CompletedTask;
        }

        try
        {
            logger.Information("New message received: {FileName}", @event.NewMessage.Name);
            NewMessageReceived?.Invoke(this, @event);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to handle new message notification");
        }

        return Task.CompletedTask;
    }

    #region Begin Static Container Registrations

    [UsedImplicitly]
    private static void Register(ContainerBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.RegisterType<NewMessageNotificationService>().AsImplementedInterfaces().AsSelf().SingleInstance();
    }

    #endregion
}
