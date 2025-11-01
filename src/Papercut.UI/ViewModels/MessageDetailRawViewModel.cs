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


using ICSharpCode.AvalonEdit.Document;

using Papercut.Message.Helpers;
using Papercut.Views;

namespace Papercut.ViewModels;

public class MessageDetailRawViewModel : Screen,
    IMessageDetailItem,
    IHandle<ThemeChangedEvent>
{
    private readonly ILogger _logger;

    private readonly SettingsSaveDebouncer<double> _zoomSaveDebouncer;

    private bool _isLoading;

    private bool _messageLoaded;

    private IDisposable? _messageLoader;

    private MimeMessage? _mimeMessage;

    private string? _raw;

    private MessageDetailRawView? _view;

    private ZoomIndicator? _zoomIndicator;

    public MessageDetailRawViewModel(ILogger logger)
    {
        DisplayName = "Raw";
        _logger = logger;

        // Set up debounced zoom save to reduce I/O during rapid zoom changes
        _zoomSaveDebouncer = new SettingsSaveDebouncer<double>(newFontSize =>
        {
            Settings.Default.TextViewZoomFontSize = newFontSize;
            Settings.Default.Save();
        });
    }

    public string? Raw
    {
        get => _raw;
        set
        {
            _raw = value;
            NotifyOfPropertyChange(() => Raw);
        }
    }

    public MimeMessage? MimeMessage
    {
        get => _mimeMessage;
        set
        {
            _mimeMessage = value;
            NotifyOfPropertyChange(() => MimeMessage);
            MessageLoaded = false;
        }
    }

    public bool MessageLoaded
    {
        get => _messageLoaded;
        set
        {
            _messageLoaded = value;
            if (!_messageLoaded)
            {
                Raw = null;
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            NotifyOfPropertyChange(() => IsLoading);
        }
    }

    public Task HandleAsync(ThemeChangedEvent @event, CancellationToken token)
    {
        if (_view != null)
        {
            AvalonEditThemeHelper.ApplyTheme(_view.rawEdit);
        }

        return Task.CompletedTask;
    }

    private void RefreshDump()
    {
        if (MessageLoaded)
            return;

        IsLoading = true;

        if (_messageLoader != null)
        {
            _messageLoader.Dispose();
            _messageLoader = null;
        }

        _messageLoader =
            Observable.Start(() => _mimeMessage.GetStringDump())
                .SubscribeOn(TaskPoolScheduler.Default)
                .ObserveOn(Dispatcher.CurrentDispatcher)
                .Subscribe(h =>
                {
                    Raw = h;
                    MessageLoaded = true;
                });
    }

    protected override void OnViewLoaded(object view)
    {
        base.OnViewLoaded(view);

        if (view is not MessageDetailRawView typedView)
        {
            _logger.Error("Unable to locate the MessageDetailRawView to hook the Text Control");
            return;
        }

        // Store references
        _view = typedView;
        _zoomIndicator = typedView.zoomIndicator;

        // Apply theme colors
        AvalonEditThemeHelper.ApplyTheme(typedView.rawEdit);

        // Restore saved zoom level
        typedView.rawEdit.FontSize = Settings.Default.TextViewZoomFontSize;

        // Install modern search panel for Ctrl+F support
        ModernSearchPanel.Install(typedView.rawEdit);

        this.GetPropertyValues(p => p.Raw)
            .ObserveOn(Dispatcher.CurrentDispatcher)
            .Subscribe(s =>
            {
                typedView.rawEdit.Document = new TextDocument(new StringTextSource(s ?? string.Empty));
                IsLoading = false;
            });

        // Hook up zoom functionality
        typedView.rawEdit.PreviewMouseWheel += (sender, e) =>
        {
            if (ZoomHelper.IsZoomModifierPressed())
            {
                e.Handled = true;
                var newFontSize = ZoomHelper.CalculateNewZoom(
                    typedView.rawEdit.FontSize,
                    e.Delta,
                    ZoomHelper.AvalonEditZoom.Increment,
                    ZoomHelper.AvalonEditZoom.MinFontSize,
                    ZoomHelper.AvalonEditZoom.MaxFontSize);
                typedView.rawEdit.FontSize = newFontSize;

                // Debounce settings save to reduce I/O during rapid zoom changes
                _zoomSaveDebouncer.OnValueChanged(newFontSize);

                // Show zoom indicator
                _zoomIndicator?.ShowZoomFromFontSize(newFontSize, ZoomHelper.AvalonEditZoom.DefaultFontSize);
            }
        };
    }

    protected override Task OnActivateAsync(CancellationToken token)
    {
        RefreshDump();
        return base.OnActivateAsync(token);
    }
}