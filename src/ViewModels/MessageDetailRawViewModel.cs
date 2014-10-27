// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2014 Jaben Cargman
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

    using Papercut.Core.Helper;
    using Papercut.Helpers;
    using Papercut.Views;

    using Serilog;

    public class MessageDetailRawViewModel : Screen, IMessageDetailItem
    {
        bool _isLoading;

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

        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                NotifyOfPropertyChange(() => IsLoading);
            }
        }

        public MimeMessage CurrentMailMessage { get; private set; }

        public string CurrentLoadedMessageId { get; private set; }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            var typedView = view as MessageDetailRawView;

            if (typedView == null)
            {
                _logger.Error("Unable to locate the MessageDetailRawView to hook the Text Control");
                return;
            }

            this.GetPropertyValues(p => p.Raw).Subscribe(t =>
            { typedView.rawEdit.Document = new TextDocument(new StringTextSource(t ?? string.Empty)); });
        }

        protected override void OnActivate()
        {
            if (CurrentMailMessage != null && CurrentLoadedMessageId != CurrentMailMessage.MessageId)
            {
                IsLoading = true;

                Observable.Start(() => CurrentMailMessage.GetStringDump(), TaskPoolScheduler.Default)
                    .ObserveOnDispatcher()
                    .Subscribe(h =>
                    {
                        IsLoading = false;
                        CurrentLoadedMessageId = CurrentMailMessage.MessageId;
                        Raw = h;
                    });
            }

            base.OnActivate();
        }

        public void LoadMessage(MimeMessage mailMessageEx)
        {
            if (mailMessageEx == null)
                throw new ArgumentNullException("mailMessageEx");

            CurrentMailMessage = mailMessageEx;
        }
    }
}