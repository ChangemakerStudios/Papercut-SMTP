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
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;

    using Caliburn.Micro;

    using ICSharpCode.AvalonEdit.Document;

    using MimeKit;

    using Papercut.Helpers;
    using Papercut.Message.Helpers;
    using Papercut.Views;

    using Serilog;

    public class MessageDetailRawViewModel : Screen, IMessageDetailItem
    {
        bool _isLoading;

        bool _messageLoaded;

        IDisposable _messageLoader;

        MimeMessage _mimeMessage;

        string _raw;

        readonly ILogger _logger;

        public MessageDetailRawViewModel(ILogger logger)
        {
            DisplayName = "Raw";
            _logger = logger;
        }

        public string Raw
        {
            get { return _raw; }
            set
            {
                _raw = value;
                NotifyOfPropertyChange(() => Raw);
            }
        }

        public MimeMessage MimeMessage
        {
            get { return _mimeMessage; }
            set
            {
                _mimeMessage = value;
                NotifyOfPropertyChange(() => MimeMessage);
                MessageLoaded = false;
            }
        }

        public bool MessageLoaded
        {
            get { return _messageLoaded; }
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
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                NotifyOfPropertyChange(() => IsLoading);
            }
        }

        void RefreshDump()
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
                    .ObserveOnDispatcher()
                    .Subscribe(h =>
                    {
                        Raw = h;
                        MessageLoaded = true;
                    });
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            var typedView = view as MessageDetailRawView;

            if (typedView == null)
            {
                _logger.Error("Unable to locate the MessageDetailRawView to hook the Text Control");
                return;
            }

            this.GetPropertyValues(p => p.Raw)
                .ObserveOnDispatcher()
                .Subscribe(s =>
                {
                    typedView.rawEdit.Document = new TextDocument(new StringTextSource(s ?? string.Empty));
                    IsLoading = false;
                });
        }

        protected override void OnActivate()
        {
            RefreshDump();
            base.OnActivate();
        }
    }
}