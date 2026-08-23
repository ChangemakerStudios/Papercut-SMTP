// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2026 Jaben Cargman
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

using Papercut.Service.TrayNotification.Infrastructure;

namespace Papercut.Service.TrayNotification.AppLayer;

/// <summary>
/// Handles new message notifications and displays balloon tips
/// </summary>
public class NewMessageNotificationService : IDisposable
{
    private readonly MessagesHubClient _hubClient;

    private readonly ILogger _logger;

    private bool _notificationsEnabled = true;

    public NewMessageNotificationService(MessagesHubClient hubClient, ILogger logger)
    {
        _hubClient = hubClient;
        _logger = logger;

        _hubClient.NewMessageReceived += OnHubNewMessageReceived;
    }

    public event EventHandler<NewMessageDto>? NewMessageReceived;

    public bool NotificationsEnabled
    {
        get => _notificationsEnabled;
        set => _notificationsEnabled = value;
    }

    public void Dispose()
    {
        _hubClient.NewMessageReceived -= OnHubNewMessageReceived;

        GC.SuppressFinalize(this);
    }

    private void OnHubNewMessageReceived(object? sender, NewMessageDto message)
    {
        if (!_notificationsEnabled)
        {
            _logger.Debug("Notifications disabled, skipping notification for message");
            return;
        }

        try
        {
            _logger.Information("New message received: {Subject}", message.Subject);
            NewMessageReceived?.Invoke(this, message);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to handle new message notification");
        }
    }

    #region Begin Static Container Registrations

    [UsedImplicitly]
    private static void Register(ContainerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RegisterType<NewMessageNotificationService>().AsImplementedInterfaces().AsSelf().SingleInstance();
    }

    #endregion
}
