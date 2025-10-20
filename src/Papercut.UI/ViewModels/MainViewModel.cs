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


using System;
using System.Reactive.Threading.Tasks;
using System.Windows.Forms;

using ICSharpCode.AvalonEdit.Utils;

using MahApps.Metro.Controls.Dialogs;

using Papercut.AppLayer.LogSinks;
using Papercut.AppLayer.NewVersionCheck;
using Papercut.AppLayer.Uris;
using Papercut.Core;
using Papercut.Core.Domain.Network.Smtp;
using Papercut.Core.Infrastructure.Async;
using Papercut.Domain.AppCommands;
using Papercut.Domain.UiCommands;
using Papercut.Domain.UiCommands.Commands;
using Papercut.Infrastructure.Resources;
using Papercut.Infrastructure.WebView;
using Papercut.Rules.App.Forwarding;
using Papercut.Rules.App.Relaying;
using Papercut.Rules.Domain.Forwarding;
using Papercut.Views;

using Serilog.Events;

using Velopack;

using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace Papercut.ViewModels;

public class MainViewModel : Conductor<object>,
    IHandle<SmtpServerBindFailedEvent>
{
    const string WindowTitleDefault = AppConstants.ApplicationName;

    private readonly IAppCommandHub _appCommandHub;

    readonly ForwardRuleDispatch _forwardRuleDispatch;

    readonly AppResourceLocator _resourceLocator;

    private readonly IUiCommandHub _uiCommandHub;

    private readonly INewVersionProvider _newVersionProvider;
    private readonly ILogger _logger;

    private readonly UpdateManager _updateManager;

    readonly UiLogSinkQueue _uiLogSinkQueue;

    readonly IViewModelWindowManager _viewModelWindowManager;

    private readonly WebView2Information _webView2Information;

    bool _isDeactivated;

    private bool _isDeleteAllConfirmOpen;

    bool _isLogOpen;

    private string? _upgradeVersion;

    private bool _isWebViewInstalled;

    string _logText;

    UpdateInfo? _updateInfo;

    MetroWindow? _window;

    string _windowTitle = WindowTitleDefault;

    public Deque<string> CurrentLogHistory = new();

    public MainViewModel(
        IViewModelWindowManager viewModelWindowManager,
        IAppCommandHub appCommandHub,
        IUiCommandHub uiCommandHub,
        INewVersionProvider newVersionProvider,
        ILogger logger,
        UpdateManager updateManager,
        WebView2Information webView2Information,
        ForwardRuleDispatch forwardRuleDispatch,
        Func<MessageListViewModel> messageListViewModelFactory,
        Func<MessageDetailViewModel> messageDetailViewModelFactory,
        UiLogSinkQueue uiLogSinkQueue,
        AppResourceLocator resourceLocator)
    {
        this._viewModelWindowManager = viewModelWindowManager;
        this._appCommandHub = appCommandHub;
        this._uiCommandHub = uiCommandHub;
        this._newVersionProvider = newVersionProvider;
        this._logger = logger;
        this._updateManager = updateManager;
        this._webView2Information = webView2Information;
        this._forwardRuleDispatch = forwardRuleDispatch;

        this.MessageListViewModel = messageListViewModelFactory();
        this.MessageDetailViewModel = messageDetailViewModelFactory();

        this.MessageListViewModel.ConductWith(this);
        this.MessageDetailViewModel.ConductWith(this);

        this._uiLogSinkQueue = uiLogSinkQueue;
        this._resourceLocator = resourceLocator;

        this.LogText = webView2Information.IsInstalled
            ? this._resourceLocator.GetResourceString("LogClientSink.html")
            : "";

        this.IsWebViewInstalled = this._webView2Information.IsInstalled;

        this.SetupObservables();
    }

    public MessageListViewModel MessageListViewModel { get; }

    public MessageDetailViewModel MessageDetailViewModel { get; }

    public bool IsWebViewInstalled
    {

        get => this._isWebViewInstalled;

        set
        {
            this._isWebViewInstalled = value;
            this.NotifyOfPropertyChange(() => this.IsWebViewInstalled);
        }
    }

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

    public string? UpgradeVersion
    {
        get => this._upgradeVersion;
        set
        {
            if (this._upgradeVersion != value)
            {
                this._upgradeVersion = value;
                this.NotifyOfPropertyChange(() => this.UpgradeVersion);
            }
        }
    }

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

    public Task HandleAsync(SmtpServerBindFailedEvent message, CancellationToken cancellationToken = default)
    {
        MessageBox.Show(
            "Failed to start SMTP server listening. The IP and Port combination is in use by another program. To fix, change the server bindings in the options.",
            "Failed");

        this.ShowOptions();

        return Task.CompletedTask;
    }

    string? GetVersion()
    {
        var productVersion =
            FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

        return productVersion?.Split('+').FirstOrDefault();
    }

    protected override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        this._newVersionProvider.GetLatestVersionAsync(cancellationToken).ToObservable()
            .Subscribe(
                updateInfo =>
                {
                    if (updateInfo != null)
                    {
                        _updateInfo = updateInfo;
                        this.UpgradeVersion = $"Upgrade available! Click here to upgrade to v{updateInfo.TargetFullRelease.Version}";
                    }
                    else
                    {
                        _updateInfo = null;
                        this.UpgradeVersion = null;
                    }
                });

        await base.OnActivateAsync(cancellationToken);
    }

    public Task ExecuteAsync(ShowMainWindowCommand command, CancellationToken cancellationToken = default)
    {
        if (this._window == null) return Task.CompletedTask;

        if (!this._window.IsVisible) this._window.Show();

        if (this._window.WindowState == WindowState.Minimized) this._window.WindowState = WindowState.Normal;

        this._window.Activate();

        this._window.Topmost = true;
        this._window.Topmost = false;

        this._window.Focus();

        if (command.SelectMostRecentMessage)
        {
            this.MessageListViewModel.SelectMostRecentMessage();
        }

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

        Debug.Assert(typedView != null, nameof(typedView) + " != null");

        if (!this._webView2Information.IsInstalled)
        {
            this.GetPropertyValues(m => m.LogText)
                .Throttle(TimeSpan.FromMilliseconds(200), TaskPoolScheduler.Default)
                .ObserveOn(Dispatcher.CurrentDispatcher)
                .Subscribe(m =>
                {
                    typedView.LogPanelNoWebView.AppendText(m);
                });
        }
        else
        {
            typedView.LogPanel.CoreWebView2InitializationCompleted += (_, _) =>
            {
                this.SetupWebView(typedView.LogPanel);
            };
        }
    }

    private void SetupWebView(WebView2Base logPanel)
    {
        logPanel.CoreWebView2.DisableEdgeFeatures();
        logPanel.NavigateToString(this.GetLogSinkHtml());

        this.GetPropertyValues(m => m.LogText)
            .Throttle(TimeSpan.FromMilliseconds(200), TaskPoolScheduler.Default)
            .ObserveOn(Dispatcher.CurrentDispatcher)
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
        if (!this._webView2Information.IsInstalled)
        {
            yield return $"[{e.Timestamp:G}][\"{e.Level}\"] {e.RenderMessage()}\r";
            if (e.Exception != null)
            {
                yield return $"Exception:</b> {e.Exception.Message}\r";
            }
        }
        else
        {
            yield return $@"<div class=""logEntry {e.Level}"">";
            yield return $@"<span class=""date"">{e.Timestamp:G}</span>";
            yield return $@"[<span class=""errorLevel"">{e.Level}</span>]";
            yield return e.RenderMessage().Linkify();
            if (e.Exception != null)
            {
                yield return
                    $@"<br/><span class=""fatal""><b>Exception:</b> {e.Exception.Message.Linkify()}</span>";
            }

            yield return @"</div>";
        }
    }

    void SetupObservables()
    {
        this.MessageListViewModel.GetPropertyValues(m => m.SelectedMessage)
            .Throttle(TimeSpan.FromMilliseconds(200), TaskPoolScheduler.Default)
            .ObserveOn(Dispatcher.CurrentDispatcher)
            .Subscribe(
                _ => this.MessageDetailViewModel.LoadMessageEntry(this.MessageListViewModel.SelectedMessage));

        // Initialize with current value before subscribing to changes
        this.MessageDetailViewModel.HasAnyMessages = this.MessageListViewModel.HasMessages;

        this.MessageListViewModel.GetPropertyValues(m => m.HasMessages)
            .ObserveOn(Dispatcher.CurrentDispatcher)
            .Subscribe(
                hasMessages => this.MessageDetailViewModel.HasAnyMessages = hasMessages);

        Observable.FromEventPattern<EventHandler, EventArgs>(
                h => new EventHandler(h),
                h => this._uiLogSinkQueue.LogEvent += h,
                h => this._uiLogSinkQueue.LogEvent -= h,
                TaskPoolScheduler.Default)
            .Buffer(TimeSpan.FromSeconds(1)) // this will cause calling the Subscribe method every second.
            .Select(
                _ =>
                {
                    return
                        this._uiLogSinkQueue.GetLastEvents()
                            .Select(e => string.Join(" ", this.RenderLogEventParts(e)))
                            .ToList();
                })
            .Where(s => s.Any())
            .ObserveOn(Dispatcher.CurrentDispatcher).Subscribe(
                o =>
                {
                    // If nothing added, return and don't process any data. And don't change LogText which would update the logs WebView2 component.
                    if(!o.Any()) { return; }

                    foreach (var s in o) this.CurrentLogHistory.PushFront(s);

                    if (this.CurrentLogHistory.Count > 150)
                    {
                        // prune
                        while (this.CurrentLogHistory.Count > 100)
                        {
                            this.CurrentLogHistory.PopBack();
                        }

                        if (!this._webView2Information.IsInstalled)
                        {
                            var logItems = this.CurrentLogHistory.ToList();
                            this.LogText = string.Join("", logItems);
                        }
                        else
                        {
                            // required pruning -- go ahead and replace the whole thing
                            var html = this.GetLogSinkHtml();
                            var logItems = this.CurrentLogHistory.ToList();
                            this.LogText = html.Replace(
                                "<body>",
                                $"<body>{string.Join("", logItems)}");
                        }
                    }
                    else
                    {
                        o.Reverse();

                        if (!this._webView2Information.IsInstalled)
                        {
                            this.LogText = string.Join("", o);
                        }
                        else
                        {                                
                            this.LogText = this.LogText.Replace(
                                "<body>",
                                $"<body>{string.Join("", o)}");
                        }
                    }
                });

        this.GetPropertyValues(m => m.IsLogOpen)
            .ObserveOn(Dispatcher.CurrentDispatcher)
            .Subscribe(this.SetIsLoading);

        this.GetPropertyValues(m => m.IsDeleteAllConfirmOpen)
            .ObserveOn(Dispatcher.CurrentDispatcher)
            .Subscribe(this.SetIsLoading);

        this._uiCommandHub.OnShowMainWindow.ObserveOn(Dispatcher.CurrentDispatcher)
            .SubscribeAsync(async c => await this.ExecuteAsync(c));

        this._uiCommandHub.OnShowMessage.ObserveOn(Dispatcher.CurrentDispatcher)
            .SubscribeAsync(async c => await this.ExecuteAsync(c));

        this._uiCommandHub.OnShowOptionWindow.ObserveOn(Dispatcher.CurrentDispatcher)
            .SubscribeAsync(async c => await this.ExecuteAsync(c));
    }

    public async Task UpgradeToLatest()
    {
        if (this._updateInfo == null)
        {
            await this.ShowMessageAsync("Update Failure", "Missing Update Information.");
            return;
        }

        if (!this._updateManager.IsInstalled)
        {
            this._logger.Warning("Cannot upgrade - application was not installed via Velopack");
            await this.ShowMessageAsync("Update Not Available",
                "Updates are only available for installations performed through the installer.");
            return;
        }

        using var cancellationSource = new CancellationTokenSource();

        var progressDialog = await this.ShowProgress("Updating", "Downloading Updates...", true,
            cancellationSource);

        try
        {
            this._logger.Information("Starting update download for version {Version}", this._updateInfo.TargetFullRelease.Version);

            // download new version with progress reporting
            await this._updateManager.DownloadUpdatesAsync(this._updateInfo,
                progress: p =>
                {
                    progressDialog.SetMessage($"Downloading Updates... {p}%");
                    progressDialog.SetProgress(p / 100.0);
                },
                cancelToken: cancellationSource.Token);

            this._logger.Information("Update download completed successfully");

            await progressDialog.CloseAsync();

            this._logger.Information("Applying updates and restarting application");

            // install new version and restart app
            this._updateManager.ApplyUpdatesAndRestart(this._updateInfo);
        }
        catch (OperationCanceledException)
        {
            this._logger.Information("Update download was cancelled by user");

            await progressDialog.CloseAsync();

            await this.ShowMessageAsync("Update Cancelled", "The update download was cancelled.");
        }
        catch (Exception ex)
        {
            this._logger.Error(ex, "Update failed: {Message}", ex.Message);

            await progressDialog.CloseAsync();

            await this.ShowMessageAsync("Update Failed",
                $"Failure during update: {ex.Message}\n\nPlease try again later or download the latest version manually from the website.");
        }
    }

    public void GoToSite()
    {
        new Uri("https://github.com/ChangemakerStudios/Papercut-SMTP").OpenUri();
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

    public async Task<ProgressDialogController> ShowProgress(string title, string message, bool allowCancellation = false, CancellationTokenSource? tokenSource = null)
    {
        this.SetIsLoading(true);

        var progressDialog = await this._window.ShowProgressAsync(title, message);

        if (allowCancellation && tokenSource == null)
        {
            throw new ArgumentNullException(nameof(tokenSource),
                "If Allow Cancellation is true, Token Source must not be null");
        }

        progressDialog.SetCancelable(allowCancellation);
        progressDialog.SetIndeterminate();

        progressDialog.Canceled += (_, _) => tokenSource?.Cancel();
        progressDialog.Closed += (_, _) => this.SetIsLoading(false);

        return progressDialog;
    }

    public async Task<MessageDialogResult> ShowMessageAsync(string title, string message)
    {
        this.SetIsLoading(true);

        try
        {
            return await this._window.ShowMessageAsync(title, message);
        }
        finally
        {
            this.SetIsLoading(false);
        }
    }

    public async Task ForwardSelected()
    {
        if (this.MessageListViewModel.SelectedMessage == null) return;

        var forwardViewModel = new ForwardViewModel {FromSetting = true};
        bool? result = await this._viewModelWindowManager.ShowDialogAsync(forwardViewModel);
        if (result == null || !result.Value) return;

        var progressDialog = await this.ShowProgress("Forwarding Email...", "Please wait");

        try
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
        }
        finally
        {
            await progressDialog.CloseAsync();
        }
    }

    protected override void OnViewAttached(object view, object context)
    {
        base.OnViewAttached(view, context);

        this._window = view as MainView;

        if (this._window == null) return;

        //_window.Flyouts.FindChild<FlyoutsControl>("LogFlyouts")

        this._window.StateChanged += (_, _) =>
        {
            if (this._window.WindowState == WindowState.Minimized && Settings.Default.MinimizeToTray)
            {
                // Hide the window if minimized so it doesn't show up on the task bar
                this._window.Hide();
            }
        };

        this._window.Closing += (_, args) =>
        {
            if (Application.Current.ShutdownMode == ShutdownMode.OnExplicitShutdown) return;

            // Cancel close and minimize if setting is set to minimize on close
            if (Settings.Default.MinimizeOnClose)
            {
                args.Cancel = true;
                this._window.WindowState = WindowState.Minimized;
            }
        };

        this._window.Activated += (_, _) => this.IsDeactivated = false;
        this._window.Deactivated += (_, _) => this.IsDeactivated = true;

        // Minimize if set to
        if (Settings.Default.StartMinimized)
        {
            bool initialWindowActivate = true;
            this._window.Activated += (_, _) =>
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