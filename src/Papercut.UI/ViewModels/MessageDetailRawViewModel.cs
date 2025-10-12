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

public class MessageDetailRawViewModel : Screen, IMessageDetailItem
{
    readonly ILogger _logger;

    bool _isLoading;

    bool _messageLoaded;

    IDisposable? _messageLoader;

    MimeMessage? _mimeMessage;

    string? _raw;

    public MessageDetailRawViewModel(ILogger logger)
    {
        this.DisplayName = "Raw";
        this._logger = logger;
    }

    public string? Raw
    {
        get => this._raw;
        set
        {
            this._raw = value;
            this.NotifyOfPropertyChange(() => this.Raw);
        }
    }

    public MimeMessage? MimeMessage
    {
        get => this._mimeMessage;
        set
        {
            this._mimeMessage = value;
            this.NotifyOfPropertyChange(() => this.MimeMessage);
            this.MessageLoaded = false;
        }
    }

    public bool MessageLoaded
    {
        get => this._messageLoaded;
        set
        {
            this._messageLoaded = value;
            if (!this._messageLoaded)
            {
                this.Raw = null;
            }
        }
    }

    public bool IsLoading
    {
        get => this._isLoading;
        set
        {
            this._isLoading = value;
            this.NotifyOfPropertyChange(() => this.IsLoading);
        }
    }

    void RefreshDump()
    {
        if (this.MessageLoaded)
            return;

        this.IsLoading = true;

        if (this._messageLoader != null)
        {
            this._messageLoader.Dispose();
            this._messageLoader = null;
        }

        this._messageLoader =
            Observable.Start(() => this._mimeMessage.GetStringDump())
                .SubscribeOn(TaskPoolScheduler.Default)
                .ObserveOn(Dispatcher.CurrentDispatcher)
                .Subscribe(h =>
                {
                    this.Raw = h;
                    this.MessageLoaded = true;
                });
    }

    protected override void OnViewLoaded(object view)
    {
        base.OnViewLoaded(view);

        if (view is not MessageDetailRawView typedView)
        {
            this._logger.Error("Unable to locate the MessageDetailRawView to hook the Text Control");
            return;
        }

        this.GetPropertyValues(p => p.Raw)
            .ObserveOn(Dispatcher.CurrentDispatcher)
            .Subscribe(s =>
            {
                typedView.rawEdit.Document = new TextDocument(new StringTextSource(s ?? string.Empty));
                this.IsLoading = false;
            });
    }

    protected override Task OnActivateAsync(CancellationToken token)
    {
        this.RefreshDump();
        return base.OnActivateAsync(token);
    }
}