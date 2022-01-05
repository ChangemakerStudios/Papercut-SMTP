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


namespace Papercut.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;

    using Caliburn.Micro;

    using ICSharpCode.AvalonEdit.Utils;

    using MahApps.Metro.Controls;
    using MahApps.Metro.Controls.Dialogs;

    using Papercut.AppLayer.LogSinks;
    using Papercut.Common.Domain;
    using Papercut.Core;
    using Papercut.Core.Domain.Network.Smtp;
    using Papercut.Domain.AppCommands;
    using Papercut.Domain.UiCommands;
    using Papercut.Domain.UiCommands.Commands;
    using Papercut.Helpers;
    using Papercut.Infrastructure.Resources;
    using Papercut.Properties;
    using Papercut.Rules.Domain.Forwarding;
    using Papercut.Rules.Domain.Relaying;
    using Papercut.Views;

    using Serilog.Events;

    public class MainViewModel : Conductor<object>,
        IHandle<SmtpServerBindFailedEvent>
    {
        const string WindowTitleDefault = AppConstants.ApplicationName;

        private readonly IAppCommandHub _appCommandHub;

        readonly ForwardRuleDispatch _forwardRuleDispatch;

        readonly AppResourceLocator _resourceLocator;

        private readonly IUiCommandHub _uiCommandHub;

        readonly UiLogSinkQueue _uiLogSinkQueue;

        readonly IViewModelWindowManager _viewModelWindowManager;

        public Deque<string> CurrentLogHistory = new Deque<string>();

        bool _isDeactivated;

        private bool _isDeleteAllConfirmOpen;

        bool _isLogOpen;

        string _logText;

        private int _mainWindowHeight;

        private int _mainWindowWidth;

        MetroWindow _window;

        string _windowTitle = WindowTitleDefault;

        public MainViewModel(
            IViewModelWindowManager viewModelWindowManager,
            IAppCommandHub appCommandHub,
            IUiCommandHub uiCommandHub,
            ForwardRuleDispatch forwardRuleDispatch,
            Func<MessageListViewModel> messageListViewModelFactory,
            Func<MessageDetailViewModel> messageDetailViewModelFactory,
            UiLogSinkQueue uiLogSinkQueue,
            AppResourceLocator resourceLocator)
        {
            this._viewModelWindowManager = viewModelWindowManager;
            this._appCommandHub = appCommandHub;
            this._uiCommandHub = uiCommandHub;
            this._forwardRuleDispatch = forwardRuleDispatch;

            this.MessageListViewModel = messageListViewModelFactory();
            this.MessageDetailViewModel = messageDetailViewModelFactory();

            this.MessageListViewModel.ConductWith(this);
            this.MessageDetailViewModel.ConductWith(this);

            this._uiLogSinkQueue = uiLogSinkQueue;
            this._resourceLocator = resourceLocator;

            this.LogText = this._resourceLocator.GetResourceString("LogClientSink.html");

            this.SetupObservables();
        }

        public MessageListViewModel MessageListViewModel { get; }

        public MessageDetailViewModel MessageDetailViewModel { get; }

        public string LogText
        {
            get => this._logText;
            set
            {
                this._logText = value;
                this.NotifyOfPropertyChange(() => this.LogText);
            }
        }

        public bool IsDeactivated
        {
            get => this._isDeactivated;
            set
            {
                this._isDeactivated = value;
                this.NotifyOfPropertyChange(() => this.IsDeactivated);
            }
        }

        public string WindowTitle
        {
            get => this._windowTitle;
            set
            {
                this._windowTitle = value;
                this.NotifyOfPropertyChange(() => this.WindowTitle);
            }
        }

        public string Version => $"{AppConstants.ApplicationName} v{this.GetVersion()}";

        public bool IsLogOpen
        {
            get => this._isLogOpen;
            set
            {
                if (this._isLogOpen != value)
                {
                    this._isLogOpen = value;
                    this.NotifyOfPropertyChange(() => this.IsLogOpen);
                }
            }
        }

        public bool IsDeleteAllConfirmOpen
        {
            get => this._isDeleteAllConfirmOpen;
            set
            {
                if (this._isDeleteAllConfirmOpen != value)
                {
                    this._isDeleteAllConfirmOpen = value;
                    this.NotifyOfPropertyChange(() => this.IsDeleteAllConfirmOpen);
                }
            }
        }

        public int MainWindowHeight
        {
            get => this._mainWindowHeight;
            set
            {
                if (value == this._mainWindowHeight) return;

                // ignore non-normal window sizes
                if (this._window.WindowState != WindowState.Normal) return;

                this._mainWindowHeight = value;
                this.NotifyOfPropertyChange(() => this.MainWindowHeight);
            }
        }

        public int MainWindowWidth
        {
            get => this._mainWindowWidth;
            set
            {
                if (value == this._mainWindowWidth) return;

                // ignore non-normal window sizes
                if (this._window.WindowState != WindowState.Normal) return;

                this._mainWindowWidth = value;
                this.NotifyOfPropertyChange(() => this.MainWindowWidth);
            }
        }

        public Task HandleAsync(SmtpServerBindFailedEvent message, CancellationToken cancellationToken = default)
        {
            MessageBox.Show(
                "Failed to start SMTP server listening. The IP and Port combination is in use by another program. To fix, change the server bindings in the options.",
                "Failed");

            this.ShowOptions();

            return Task.CompletedTask;
        }

        string GetVersion()
        {
            var productVersion =
                FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

            return productVersion.Split('+').FirstOrDefault();
        }

        public Task ExecuteAsync(ShowMainWindowCommand command, CancellationToken cancellationToken = default)
        {
            if (!this._window.IsVisible) this._window.Show();

            if (this._window.WindowState == WindowState.Minimized) this._window.WindowState = WindowState.Normal;

            this._window.Activate();

            this._window.Topmost = true;
            this._window.Topmost = false;

            this._window.Focus();

            if (command.SelectMostRecentMessage) this.MessageListViewModel.TryGetValidSelectedIndex();

            return Task.CompletedTask;
        }

        public async Task ExecuteAsync(
            ShowMessageCommand message,
            CancellationToken cancellationToken = default)
        {
            this.MessageDetailViewModel.IsLoading = true;
            await this._window.ShowMessageAsync(message.Caption, message.MessageText);
            this.MessageDetailViewModel.IsLoading = false;
        }

        public Task ExecuteAsync(ShowOptionWindowCommand message, CancellationToken cancellationToken = default)
        {
            this.ShowOptions();

            return Task.CompletedTask;
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            var typedView = view as MainView;

            typedView.LogPanel.CoreWebView2InitializationCompleted += (sender, args) =>
            {
                this.SetupWebView(typedView.LogPanel);
            };
        }

        private void SetupWebView(WebView2Base logPanel)
        {
            logPanel.CoreWebView2.DisableEdgeFeatures();
            logPanel.NavigateToString(GetLogSinkHtml());

            this.GetPropertyValues(m => m.LogText)
                .Throttle(TimeSpan.FromMilliseconds(200), TaskPoolScheduler.Default)
                .ObserveOnDispatcher()
                .Subscribe(m =>
                {
                    logPanel.NavigateToString(m);
                });
        }

        private string GetLogSinkHtml()
        {
            return this._resourceLocator.GetResourceString("LogClientSink.html");
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

        void SetupObservables()
        {
            this.MessageListViewModel.GetPropertyValues(m => m.SelectedMessage)
                .Throttle(TimeSpan.FromMilliseconds(200), TaskPoolScheduler.Default)
                .ObserveOnDispatcher()
                .Subscribe(
                    m => this.MessageDetailViewModel.LoadMessageEntry(this.MessageListViewModel.SelectedMessage));

            Observable.FromEventPattern<EventHandler, EventArgs>(
                    h => new EventHandler(h),
                    h => this._uiLogSinkQueue.LogEvent += h,
                    h => this._uiLogSinkQueue.LogEvent -= h,
                    TaskPoolScheduler.Default)
                .Buffer(TimeSpan.FromSeconds(1))
                .Select(
                    s =>
                    {
                        return
                            this._uiLogSinkQueue.GetLastEvents()
                                .Select(e => string.Join(" ", this.RenderLogEventParts(e)))
                                .ToList();
                    })
                .ObserveOnDispatcher().Subscribe(
                    o =>
                    {
                        foreach (var s in o) this.CurrentLogHistory.PushFront(s);

                        if (this.CurrentLogHistory.Count > 150)
                        {
                            // prune
                            while (this.CurrentLogHistory.Count > 100)
                            {
                                this.CurrentLogHistory.PopBack();
                            }

                            // required pruning -- go ahead and replace the whole thing
                            var html = this.GetLogSinkHtml();
                            var logItems = this.CurrentLogHistory.ToList();
                            this.LogText = html.Replace(
                                "<body>",
                                $"<body>{string.Join("", logItems)}");
                        }
                        else
                        {
                            o.Reverse();

                            this.LogText = this.LogText.Replace(
                                "<body>",
                                $"<body>{string.Join("", o)}");
                        }
                    });

            this.GetPropertyValues(m => m.IsLogOpen)
                .ObserveOnDispatcher()
                .Subscribe(this.SetIsLoading);

            this.GetPropertyValues(m => m.IsDeleteAllConfirmOpen)
                .ObserveOnDispatcher()
                .Subscribe(this.SetIsLoading);

            this._uiCommandHub.OnShowMainWindow.ObserveOnDispatcher()
                .Subscribe(async c => await this.ExecuteAsync(c));

            this._uiCommandHub.OnShowMessage.ObserveOnDispatcher()
                .Subscribe(async c => await this.ExecuteAsync(c));

            this._uiCommandHub.OnShowOptionWindow.ObserveOnDispatcher()
                .Subscribe(async c => await this.ExecuteAsync(c));
        }

        public void GoToSite()
        {
            Process.Start("https://github.com/ChangemakerStudios/Papercut-SMTP");
        }

        public void ShowRulesConfiguration()
        {
            this.CloseFlyouts();

            this._viewModelWindowManager.ShowDialogWithViewModel<RulesConfigurationViewModel>();
        }

        public void DeleteAll()
        {
            this.MessageListViewModel.DeleteAll();
            this.IsDeleteAllConfirmOpen = false;
        }

        public void CancelDeleteAll()
        {
            this.IsDeleteAllConfirmOpen = false;
        }

        public void ShowConfirmDeleteAll()
        {
            this.CloseFlyouts();
            this.IsDeleteAllConfirmOpen = true;
        }

        public void ToggleLog()
        {
            if (!this.IsLogOpen)
            {
                this.CloseFlyouts();
            }

            this.IsLogOpen = !this.IsLogOpen;
        }

        public void ShowOptions()
        {
            this.CloseFlyouts();

            this._viewModelWindowManager.ShowDialogWithViewModel<OptionsViewModel>();
        }

        private void CloseFlyouts()
        {
            this.IsLogOpen = false;
            this.IsDeleteAllConfirmOpen = false;
        }

        public void Exit()
        {
            this._appCommandHub.Shutdown();
        }

        private void SetIsLoading(bool isLoading)
        {
            this.MessageListViewModel.IsLoading = isLoading;
            this.MessageDetailViewModel.IsLoading = isLoading;
        }

        public async Task<ProgressDialogController> ShowForwardingEmailProgress()
        {
            this.SetIsLoading(true);

            Task<ProgressDialogController> progressController = this._window.ShowProgressAsync("Forwarding Email...", "Please wait");

            ProgressDialogController progressDialog = await progressController;

            progressDialog.SetCancelable(false);
            progressDialog.SetIndeterminate();

            progressDialog.Closed += (sender, args) => this.SetIsLoading(false);

            return progressDialog;
        }

        public async Task ForwardSelected()
        {
            if (this.MessageListViewModel.SelectedMessage == null) return;

            var forwardViewModel = new ForwardViewModel {FromSetting = true};
            bool? result = await this._viewModelWindowManager.ShowDialogAsync(forwardViewModel);
            if (result == null || !result.Value) return;

            var progressDialog = await ShowForwardingEmailProgress();

            Observable.Start(
                    async () =>
                    {
                        var forwardRule = new ForwardRule
                                          {
                                              FromEmail = forwardViewModel.From,
                                              ToEmail = forwardViewModel.To
                                          };

                        forwardRule.PopulateServerFromUri(forwardViewModel.Server);

                        // send message using relay dispatcher...
                        await this._forwardRuleDispatch.DispatchAsync(
                            forwardRule,
                            this.MessageListViewModel.SelectedMessage);

                        return true;
                    },
                    TaskPoolScheduler.Default)
                .ObserveOnDispatcher()
                .Subscribe(async b => await progressDialog.CloseAsync());
        }

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);

            this._window = view as MainView;

            if (this._window == null) return;

            //_window.Flyouts.FindChild<FlyoutsControl>("LogFlyouts")

            this._window.StateChanged += (sender, args) =>
            {
                if (this._window.WindowState == WindowState.Minimized && Settings.Default.MinimizeToTray)
                {
                    // Hide the window if minimized so it doesn't show up on the task bar
                    this._window.Hide();
                }
            };

            this._window.Closing += (sender, args) =>
            {
                if (Application.Current.ShutdownMode == ShutdownMode.OnExplicitShutdown) return;

                // Cancel close and minimize if setting is set to minimize on close
                if (Settings.Default.MinimizeOnClose)
                {
                    args.Cancel = true;
                    this._window.WindowState = WindowState.Minimized;
                }
            };

            this._window.Activated += (sender, args) => this.IsDeactivated = false;
            this._window.Deactivated += (sender, args) => this.IsDeactivated = true;

            // Minimize if set to
            if (Settings.Default.StartMinimized)
            {
                bool initialWindowActivate = true;
                this._window.Activated += (sender, args) =>
                {
                    if (initialWindowActivate)
                    {
                        initialWindowActivate = false;
                        this._window.WindowState = WindowState.Minimized;
                    }
                };
            }
        }
    }
}