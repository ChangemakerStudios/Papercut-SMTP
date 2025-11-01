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

using Papercut.Views;

namespace Papercut.ViewModels;

public sealed class MessageDetailBodyViewModel : Screen,
    IMessageDetailItem,
    IHandle<ThemeChangedEvent>
{
    private readonly ILogger _logger;

    private readonly SettingsSaveDebouncer<double> _zoomSaveDebouncer;

    private string? _body;

    private MessageDetailBodyView? _view;

    private ZoomIndicator? _zoomIndicator;

    public MessageDetailBodyViewModel(ILogger logger)
    {
        _logger = logger;
        DisplayName = "Body";

        // Set up debounced zoom save to reduce I/O during rapid zoom changes
        _zoomSaveDebouncer = new SettingsSaveDebouncer<double>(newFontSize =>
        {
            Settings.Default.TextViewZoomFontSize = newFontSize;
            Settings.Default.Save();
        });
    }

    public string? Body
    {
        get => _body;
        set
        {
            _body = value;
            NotifyOfPropertyChange(() => Body);
        }
    }

    public Task HandleAsync(ThemeChangedEvent @event, CancellationToken token)
    {
        if (_view != null)
        {
            AvalonEditThemeHelper.ApplyTheme(_view.BodyEdit);
        }

        return Task.CompletedTask;
    }

    protected override void OnViewLoaded(object view)
    {
        base.OnViewLoaded(view);

        if (!(view is MessageDetailBodyView typedView))
        {
            _logger.Error("Unable to locate the MessageDetailBodyView to hook the Text Control");
            return;
        }

        // Store references
        _view = typedView;
        _zoomIndicator = typedView.zoomIndicator;

        // Apply theme colors
        AvalonEditThemeHelper.ApplyTheme(typedView.BodyEdit);

        // Restore saved zoom level
        typedView.BodyEdit.FontSize = Settings.Default.TextViewZoomFontSize;

        // Install modern search panel for Ctrl+F support
        ModernSearchPanel.Install(typedView.BodyEdit);

        this.GetPropertyValues(p => p.Body)
            .Subscribe(
                t => { typedView.BodyEdit.Document = new TextDocument(new StringTextSource(t ?? string.Empty)); });

        // Hook up zoom functionality
        typedView.BodyEdit.PreviewMouseWheel += (sender, e) =>
        {
            if (ZoomHelper.IsZoomModifierPressed())
            {
                e.Handled = true;
                var newFontSize = ZoomHelper.CalculateNewZoom(
                    typedView.BodyEdit.FontSize,
                    e.Delta,
                    ZoomHelper.AvalonEditZoom.Increment,
                    ZoomHelper.AvalonEditZoom.MinFontSize,
                    ZoomHelper.AvalonEditZoom.MaxFontSize);
                typedView.BodyEdit.FontSize = newFontSize;

                // Debounce settings save to reduce I/O during rapid zoom changes
                _zoomSaveDebouncer.OnValueChanged(newFontSize);

                // Show zoom indicator
                _zoomIndicator?.ShowZoomFromFontSize(newFontSize, ZoomHelper.AvalonEditZoom.DefaultFontSize);
            }
        };
    }
}