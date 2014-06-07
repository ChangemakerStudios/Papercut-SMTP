/*  
 * Papercut
 *
 *  Copyright © 2008 - 2012 Ken Robertson
 *  Copyright © 2013 - 2014 Jaben Cargman
 *  
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *  
 *  http://www.apache.org/licenses/LICENSE-2.0
 *  
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *  
 */

namespace Papercut.ViewModels
{
    using System;
    using System.Diagnostics;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Reflection;
    using System.Windows;

    using Caliburn.Micro;

    using Papercut.Core.Events;
    using Papercut.Core.Message;
    using Papercut.Events;
    using Papercut.Helpers;
    using Papercut.Properties;

    public class MainViewModel : Screen,
        IHandle<SmtpServerBindFailedEvent>,
        IHandle<ShowMessageEvent>,
        IHandle<ShowMainWindowEvent>
    {
        const string WindowTitleDefault = "Papercut";

        readonly Func<ForwardViewModel> _forwardViewModelFactory;

        readonly MimeMessageLoader _mimeMessageLoader;

        readonly Func<OptionsViewModel> _optionsViewModelFactory;

        readonly IPublishEvent _publishEvent;

        readonly IWindowManager _windowsManager;

        IDisposable _loadingDisposable;

        Window _window;

        string _windowTitle = WindowTitleDefault;

        public MainViewModel(
            IWindowManager windowsManager,
            IPublishEvent publishEvent,
            Func<OptionsViewModel> optionsViewModelFactory,
            Func<MessageListViewModel> messageListViewModelFactory,
            Func<MessageDetailViewModel> messageDetailViewModelFactory,
            Func<ForwardViewModel> forwardViewModelFactory,
            MimeMessageLoader mimeMessageLoader)
        {
            _windowsManager = windowsManager;
            _publishEvent = publishEvent;
            _optionsViewModelFactory = optionsViewModelFactory;
            _forwardViewModelFactory = forwardViewModelFactory;
            _mimeMessageLoader = mimeMessageLoader;

            MessageListViewModel = messageListViewModelFactory();
            MessageDetailViewModel = messageDetailViewModelFactory();

            SetupObservables();
        }

        public MessageListViewModel MessageListViewModel { get; private set; }

        public MessageDetailViewModel MessageDetailViewModel { get; private set; }

        public string WindowTitle
        {
            get
            {
                return _windowTitle;
            }
            set
            {
                _windowTitle = value;
                NotifyOfPropertyChange(() => WindowTitle);
            }
        }

        public string Version
        {
            get
            {
                return string.Format(
                    "Papercut v{0}",
                    Assembly.GetExecutingAssembly().GetName().Version.ToString(3));
            }
        }

        void IHandle<ShowMainWindowEvent>.Handle(ShowMainWindowEvent message)
        {
            if (!_window.IsVisible) _window.Show();

            if (_window.WindowState == WindowState.Minimized) _window.WindowState = WindowState.Normal;

            _window.Activate();

            _window.Topmost = true;
            _window.Topmost = false;

            _window.Focus();

            if (message.SelectMostRecentMessage) MessageListViewModel.SetSelectedIndex();
        }

        void IHandle<ShowMessageEvent>.Handle(ShowMessageEvent message)
        {
            MessageBox.Show(message.MessageText, message.Caption);
        }

        void IHandle<SmtpServerBindFailedEvent>.Handle(SmtpServerBindFailedEvent message)
        {
            MessageBox.Show(
                "Failed to start SMTP server listening. The IP and Port combination is in use by another program. To fix, change the server bindings in the options.",
                "Failed");

            ShowOptions();
        }

        void SetupObservables()
        {
            MessageListViewModel.GetPropertyValues(m => m.SelectedMessage)
                .Throttle(TimeSpan.FromMilliseconds(200), TaskPoolScheduler.Default)
                .ObserveOnDispatcher()
                .Subscribe(m => LoadMessageEntry(MessageListViewModel.SelectedMessage));
        }

        public void LoadMessageEntry(MessageEntry messageEntry)
        {
            if (_loadingDisposable != null) _loadingDisposable.Dispose();

            if (messageEntry == null)
            {
                // show empty...
                MessageDetailViewModel.DisplayMimeMessage(null);
            }
            else
            {
                // load and show it...
                _loadingDisposable =
                    _mimeMessageLoader.Get(messageEntry)
                        .ObserveOnDispatcher()
                        .Subscribe(MessageDetailViewModel.DisplayMimeMessage);
            }
        }

        public void GoToSite()
        {
            Process.Start("http://papercut.codeplex.com/");
        }

        public void ShowOptions()
        {
            _windowsManager.ShowDialog(_optionsViewModelFactory());
        }

        public void Exit()
        {
            _publishEvent.Publish(new AppForceShutdownEvent());
        }

        public void ForwardSelected()
        {
            MessageEntry entry = MessageListViewModel.SelectedMessage;
            if (entry != null)
            {
                ForwardViewModel forwardViewModel = _forwardViewModelFactory();
                forwardViewModel.MessageEntry = entry;
                _windowsManager.ShowDialog(forwardViewModel);
            }
        }

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);

            _window = view as Window;

            if (_window == null) return;

            _window.StateChanged += (sender, args) =>
            {
                // Hide the window if minimized so it doesn't show up on the task bar
                if (_window.WindowState == WindowState.Minimized) _window.Hide();
            };

            _window.Closing += (sender, args) =>
            {
                if (Application.Current.ShutdownMode == ShutdownMode.OnExplicitShutdown) return;

                // Cancel close and minimize if setting is set to minimize on close
                if (Settings.Default.MinimizeOnClose)
                {
                    args.Cancel = true;
                    _window.WindowState = WindowState.Minimized;
                }
            };

            // Minimize if set to
            if (Settings.Default.StartMinimized)
            {
                bool initialWindowActivate = true;
                _window.Activated += (sender, args) =>
                {
                    if (initialWindowActivate)
                    {
                        initialWindowActivate = false;
                        _window.WindowState = WindowState.Minimized;
                    }
                };
            }
        }
    }
}