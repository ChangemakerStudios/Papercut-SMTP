// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2021 Jaben Cargman
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


namespace Papercut.AppLayer.UiCommands
{
    using System;
    using System.Reactive.Subjects;
    using System.Windows.Forms;

    using Autofac;
    using Autofac.Util;

    using Papercut.Core.Annotations;
    using Papercut.Domain.UiCommands;
    using Papercut.Domain.UiCommands.Commands;

    public class UiCommandHub : Disposable, IUiCommandHub
    {
        private readonly Subject<ShowBalloonTipCommand> _onShowBalloonTip = new Subject<ShowBalloonTipCommand>();

        private readonly Subject<ShowMainWindowCommand> _onShowMainWindow = new Subject<ShowMainWindowCommand>();

        private readonly Subject<ShowMessageCommand> _onShowMessage = new Subject<ShowMessageCommand>();

        private readonly Subject<ShowOptionWindowCommand> _onShowOptionWindow = new Subject<ShowOptionWindowCommand>();

        public IObservable<ShowBalloonTipCommand> OnShowBalloonTip => this._onShowBalloonTip;

        public IObservable<ShowOptionWindowCommand> OnShowOptionWindow => this._onShowOptionWindow;

        public IObservable<ShowMessageCommand> OnShowMessage => this._onShowMessage;

        public IObservable<ShowMainWindowCommand> OnShowMainWindow => this._onShowMainWindow;

        public void ShowMainWindow(bool selectMostRecentMessage = false)
        {
            var command = new ShowMainWindowCommand(selectMostRecentMessage);

            this._onShowMainWindow.OnNext(command);
        }

        public void ShowMessage(string messageText, string caption)
        {
            var command = new ShowMessageCommand(messageText, caption);
            this._onShowMessage.OnNext(command);
        }

        public void ShowOptionWindow()
        {
            var command = new ShowOptionWindowCommand();
            this._onShowOptionWindow.OnNext(command);
        }

        public void ShowBalloonTip(int timeout, string tipTitle, string tipText, ToolTipIcon toolTipIcon)
        {
            if (Papercut.Properties.Settings.Default.ShowNotifications)
            {
                var command = new ShowBalloonTipCommand(timeout, tipTitle, tipText, toolTipIcon);
                this._onShowBalloonTip.OnNext(command);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this._onShowBalloonTip.Dispose();
                this._onShowMainWindow.Dispose();
                this._onShowMessage.Dispose();
                this._onShowOptionWindow.Dispose();
            }
        }

        #region Begin Static Container Registrations

        /// <summary>
        /// Called dynamically from the RegisterStaticMethods() call in the container module.
        /// </summary>
        /// <param name="builder"></param>
        [UsedImplicitly]
        static void Register([NotNull] ContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.RegisterType<UiCommandHub>().As<IUiCommandHub>().SingleInstance();
        }

        #endregion
    }
}