// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2020 Jaben Cargman
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


namespace Papercut.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Reflection;
    using System.Security.RightsManagement;
    using System.Threading.Tasks;
    using System.Windows;

    using Caliburn.Micro;

    using Common;

    using Core;

    using ICSharpCode.AvalonEdit.Utils;

    using MahApps.Metro.Controls;
    using MahApps.Metro.Controls.Dialogs;

    using Papercut.Common.Domain;
    using Papercut.Core.Domain.Network.Smtp;
    using Papercut.Core.Infrastructure.Lifecycle;
    using Papercut.Events;
    using Papercut.Helpers;
    using Papercut.Properties;
    using Papercut.Rules.Helpers;
    using Papercut.Rules.Implementations;
    using Papercut.Services;
    using Papercut.Views;

    using Serilog.Events;

    public class MainViewModel : Conductor<object>,
        IHandle<SmtpServerBindFailedEvent>,
        IHandle<ShowMessageEvent>,
        IHandle<ShowMainWindowEvent>,
        IHandle<ShowOptionWindowEvent>
    {
        const string WindowTitleDefault = AppConstants.ApplicationName;

        readonly ForwardRuleDispatch _forwardRuleDispatch;

        readonly LogClientSinkQueue _logClientSinkQueue;

        readonly IMessageBus _messageBus;

        readonly AppResourceLocator _resourceLocator;

        readonly IViewModelWindowManager _viewModelWindowManager;

        bool _isDeactivated;

        bool _isLogOpen;

        string _logText;

        MetroWindow _window;

        string _windowTitle = WindowTitleDefault;

        public MainViewModel(
            IViewModelWindowManager viewModelWindowManager,
            IMessageBus messageBus,
            ForwardRuleDispatch forwardRuleDispatch,
            Func<MessageListViewModel> messageListViewModelFactory,
            Func<MessageDetailViewModel> messageDetailViewModelFactory,
            LogClientSinkQueue logClientSinkQueue,
            AppResourceLocator resourceLocator)
        {
            _viewModelWindowManager = viewModelWindowManager;
            this._messageBus = messageBus;
            _forwardRuleDispatch = forwardRuleDispatch;

            MessageListViewModel = messageListViewModelFactory();
            MessageDetailViewModel = messageDetailViewModelFactory();

            MessageListViewModel.ConductWith(this);
            MessageDetailViewModel.ConductWith(this);

            _logClientSinkQueue = logClientSinkQueue;
            _resourceLocator = resourceLocator;

            LogText = _resourceLocator.GetResourceString("LogClientSink.html");

            SetupObservables();
        }

        public MessageListViewModel MessageListViewModel { get; private set; }

        public MessageDetailViewModel MessageDetailViewModel { get; private set; }

        public string LogText
        {
            get => _logText;
            set
            {
                _logText = value;
                NotifyOfPropertyChange(() => LogText);
            }
        }

        public bool IsDeactivated
        {
            get => _isDeactivated;
            set
            {
                _isDeactivated = value;
                NotifyOfPropertyChange(() => IsDeactivated);
            }
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                _windowTitle = value;
                NotifyOfPropertyChange(() => WindowTitle);
            }
        }

        string GetVersion()
        {
            var productVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

            return productVersion.Split('+').FirstOrDefault();
        }

        public string Version => $"{AppConstants.ApplicationName} v{GetVersion()}";

        public bool IsLogOpen
        {
            get => _isLogOpen;
            set
            {
                if (_isLogOpen != value)
                {
                    _isLogOpen = value;
                    NotifyOfPropertyChange(() => IsLogOpen);
                }
            }
        }

        public bool IsDeleteAllConfirmOpen
        {
            get => _isDeleteAllConfirmOpen;
            set
            {
                if (_isDeleteAllConfirmOpen != value)
                {
                    _isDeleteAllConfirmOpen = value;
                    NotifyOfPropertyChange(() => IsDeleteAllConfirmOpen);
                }
            }
        }

        public int MainWindowHeight
        {
            get => _mainWindowHeight;
            set
            {
                if (value == _mainWindowHeight) return;

                // ignore non-normal window sizes
                if (_window.WindowState != WindowState.Normal) return;

                _mainWindowHeight = value;
                NotifyOfPropertyChange(() => MainWindowHeight);
            }
        }

        public int MainWindowWidth
        {
            get => _mainWindowWidth;
            set
            {
                if (value == _mainWindowWidth) return;

                // ignore non-normal window sizes
                if (_window.WindowState != WindowState.Normal) return;

                _mainWindowWidth = value;
                NotifyOfPropertyChange(() => MainWindowWidth);
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

            if (message.SelectMostRecentMessage) MessageListViewModel.TryGetValidSelectedIndex();
        }

        void IHandle<ShowMessageEvent>.Handle(ShowMessageEvent message)
        {
            MessageDetailViewModel.IsLoading = true;
            _window.ShowMessageAsync(message.Caption, message.MessageText).ContinueWith(
                r =>
                {
                    var result = r.Result;
                    MessageDetailViewModel.IsLoading = false;
                },
                TaskScheduler.FromCurrentSynchronizationContext());
        }

        void IHandle<ShowOptionWindowEvent>.Handle(ShowOptionWindowEvent message)
        {
            ShowOptions();
        }

        void IHandle<SmtpServerBindFailedEvent>.Handle(SmtpServerBindFailedEvent message)
        {
            MessageBox.Show(
                "Failed to start SMTP server listening. The IP and Port combination is in use by another program. To fix, change the server bindings in the options.",
                "Failed");

            ShowOptions();
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            var typedView = view as MainView;

            var logPanel = typedView.LogPanel;
            logPanel.Text = GetLogSinkHtml();

            this.GetPropertyValues(m => m.LogText)
                .Throttle(TimeSpan.FromMilliseconds(200), TaskPoolScheduler.Default)
                .ObserveOnDispatcher()
                .Subscribe(m => logPanel.Text = m);
        }

        private string GetLogSinkHtml()
        {
            return _resourceLocator.GetResourceString("LogClientSink.html");
        }

        public IEnumerable<string> RenderLogEventParts(LogEvent e)
        {
            yield return $@"<div class=""logEntry {e.Level}"">";
            yield return $@"<span class=""date"">{e.Timestamp:G}</span>";
            yield return $@"[<span class=""errorLevel"">{e.Level}</span>]";
            yield return e.RenderMessage().Linkify();
            if (e.Exception != null)
            {
                yield return $@"<br/><span class=""fatal""><b>Exception:</b> {e.Exception.Message.Linkify()}</span>";
            }
            yield return @"</div>";
        }

        public Deque<string> _currentLogHistory = new Deque<string>();
        private bool _isDeleteAllConfirmOpen;
        private int _mainWindowWidth;
        private int _mainWindowHeight;

        void SetupObservables()
        {
            MessageListViewModel.GetPropertyValues(m => m.SelectedMessage)
                .Throttle(TimeSpan.FromMilliseconds(200), TaskPoolScheduler.Default)
                .ObserveOnDispatcher()
                .Subscribe(
                    m =>
                        MessageDetailViewModel.LoadMessageEntry(MessageListViewModel.SelectedMessage));

            Observable.FromEventPattern<EventHandler, EventArgs>(
                    h => new EventHandler(h),
                    h => _logClientSinkQueue.LogEvent += h,
                    h => _logClientSinkQueue.LogEvent -= h,
                    TaskPoolScheduler.Default)
                .Buffer(TimeSpan.FromSeconds(1))
                .Select(
                    s =>
                    {
                        return
                            _logClientSinkQueue.GetLastEvents()
                                .Select(e => string.Join(" ", RenderLogEventParts(e)))
                                .ToList();
                    })
                .ObserveOnDispatcher().Subscribe(
                    o =>
                    {
                        foreach (var s in o) _currentLogHistory.PushFront(s);

                        if (_currentLogHistory.Count > 150)
                        {
                            // prune
                            while (_currentLogHistory.Count > 100)
                            {
                                _currentLogHistory.PopBack();
                            }

                            // required pruning -- go ahead and replace the whole thing
                            var html = GetLogSinkHtml();
                            var logItems = _currentLogHistory.ToList();
                            LogText = html.Replace(
                                "<body>",
                                $"<body>{string.Join("", logItems)}");
                        }
                        else
                        {
                            o.Reverse();

                            LogText = LogText.Replace(
                                "<body>",
                                $"<body>{string.Join("", o)}");
                        }
                    });

            this.GetPropertyValues(m => m.IsLogOpen)
                .ObserveOnDispatcher()
                .Subscribe(
                    m =>
                    {
                        MessageListViewModel.IsLoading = m;
                        MessageDetailViewModel.IsLoading = m;
                    });

            this.GetPropertyValues(m => m.IsDeleteAllConfirmOpen)
                .ObserveOnDispatcher()
                .Subscribe(
                    m =>
                    {
                        MessageListViewModel.IsLoading = m;
                        MessageDetailViewModel.IsLoading = m;
                    });
        }

        public void GoToSite()
        {
            Process.Start("https://github.com/ChangemakerStudios/Papercut");
        }

        public void ShowRulesConfiguration()
        {
            CloseFlyouts();

            _viewModelWindowManager.ShowDialogWithViewModel<RulesConfigurationViewModel>();
        }

        public void DeleteAll()
        {
            MessageListViewModel.DeleteAll();
            this.IsDeleteAllConfirmOpen = false;
        }

        public void CancelDeleteAll()
        {
            this.IsDeleteAllConfirmOpen = false;
        }

        public void ShowConfirmDeleteAll()
        {
            CloseFlyouts();
            this.IsDeleteAllConfirmOpen = true;
        }

        public void ToggleLog()
        {
            if (!IsLogOpen)
            {
                CloseFlyouts();
            }

            IsLogOpen = !IsLogOpen;
        }

        public void ShowOptions()
        {
            CloseFlyouts();

            _viewModelWindowManager.ShowDialogWithViewModel<OptionsViewModel>();
        }

        private void CloseFlyouts()
        {
            IsLogOpen = false;
            IsDeleteAllConfirmOpen = false;
        }

        public void Exit()
        {
            this._messageBus.Publish(new AppForceShutdownEvent());
        }

        public void ForwardSelected()
        {
            if (MessageListViewModel.SelectedMessage == null) return;

            var forwardViewModel = new ForwardViewModel { FromSetting = true };
            bool? result = _viewModelWindowManager.ShowDialog(forwardViewModel);
            if (result == null || !result.Value) return;

            MessageDetailViewModel.IsLoading = true;
            Task<ProgressDialogController> progressController =
                _window.ShowProgressAsync("Forwarding Email...", "Please wait");

            Observable.Start(
                    () =>
                    {
                        ProgressDialogController progressDialog = progressController.Result;

                        progressDialog.SetCancelable(false);
                        progressDialog.SetIndeterminate();

                        var forwardRule = new ForwardRule
                                          {
                                              FromEmail = forwardViewModel.From,
                                              ToEmail = forwardViewModel.To
                                          };

                        forwardRule.PopulateServerFromUri(forwardViewModel.Server);

                        // send message using relay dispatcher...
                        _forwardRuleDispatch.Dispatch(
                            forwardRule,
                            MessageListViewModel.SelectedMessage);

                        progressDialog.CloseAsync().Wait();

                        return true;
                    },
                    TaskPoolScheduler.Default)
                .ObserveOnDispatcher()
                .Subscribe(b => { MessageDetailViewModel.IsLoading = false; });
        }

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);

            _window = view as MainView;

            if (_window == null) return;

            //_window.Flyouts.FindChild<FlyoutsControl>("LogFlyouts")

            _window.StateChanged += (sender, args) =>
            {
                if (_window.WindowState == WindowState.Minimized && Settings.Default.MinimizeToTray)
                {
                    // Hide the window if minimized so it doesn't show up on the task bar
                    _window.Hide();
                }
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

            _window.Activated += (sender, args) => IsDeactivated = false;
            _window.Deactivated += (sender, args) => IsDeactivated = true;

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