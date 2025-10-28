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


using System.Drawing;
using System.Windows.Forms;

using Papercut.Core;
using Papercut.Core.Infrastructure.Lifecycle;
using Papercut.Domain.AppCommands;
using Papercut.Domain.UiCommands;
using Papercut.Infrastructure.Resources;

namespace Papercut.AppLayer.Notifications;

[UsedImplicitly]
public class NotificationMenuCoordinator : Disposable, IAppLifecyclePreExit, IEventHandler<PapercutClientReadyEvent>
{
    private readonly IAppCommandHub _appCommandHub;

    readonly AppResourceLocator _resourceLocator;

    private readonly IUiCommandHub _uiCommandHub;

    NotifyIcon? _notification;

    public NotificationMenuCoordinator(
        IAppCommandHub appCommandHub,
        IUiCommandHub uiCommandHub,
        AppResourceLocator resourceLocator)
    {
        this._appCommandHub = appCommandHub;
        this._uiCommandHub = uiCommandHub;
        this._resourceLocator = resourceLocator;

        this.InitObservables();
    }

    public Task<AppLifecycleActionResultType> OnPreExit()
    {
        this.Reset();

        return Task.FromResult(AppLifecycleActionResultType.Continue);
    }

    public Task HandleAsync(PapercutClientReadyEvent @event, CancellationToken token)
    {
        if (this._notification == null) this.SetupNotification();

        return Task.CompletedTask;
    }

    void InitObservables()
    {
        this._uiCommandHub.OnShowBalloonTip
            .Sample(TimeSpan.FromSeconds(1), TaskPoolScheduler.Default)
            .ObserveOn(TaskPoolScheduler.Default)
            .Subscribe(
                @event =>
                {
                    this._notification?.ShowBalloonTip(
                        @event.Timeout,
                        @event.TipTitle,
                        @event.TipText,
                        @event.ToolTipIcon);
                });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.Reset();
        }
    }

    private void Reset()
    {
        this._notification?.Dispose();
        this._notification = null;
    }

    void SetupNotification()
    {
        // Set up the notification icon
        this._notification = new NotifyIcon
        {
            Icon = new Icon(this._resourceLocator.GetResource("App.ico").Stream),
            Text = AppConstants.ApplicationName,
            Visible = true
        };

        this._notification.Click += Notification_OnClick;
        this._notification.BalloonTipClicked +=
            (_, _) =>
            {
                this._uiCommandHub.ShowMainWindow(true);
            };

        this._notification.ContextMenuStrip = new ContextMenuStrip();

        this._notification.ContextMenuStrip.Items.Add("Show", null, (_, _) =>
        {
            this._uiCommandHub.ShowMainWindow();
        });
        this._notification.ContextMenuStrip.Items.Add("Options", null, (_, _) =>
        {
            this._uiCommandHub.ShowOptionWindow();
        });
        this._notification.ContextMenuStrip.Items.Add("Exit", null, (_, _) =>
        {
            this._appCommandHub.Shutdown();
        });
    }

    private void Notification_OnClick(object? sender, EventArgs e)
    {
        if (e is MouseEventArgs { Button: MouseButtons.Left })
        {
            this._uiCommandHub.ShowMainWindow();
        }

        this._notification?.ContextMenuStrip?.Show();
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

        builder.RegisterType<NotificationMenuCoordinator>().AsImplementedInterfaces()
            .SingleInstance();
    }

    #endregion
}