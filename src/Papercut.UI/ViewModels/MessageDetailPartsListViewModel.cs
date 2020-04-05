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
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Windows;

    using Caliburn.Micro;

    using Common.Extensions;
    using Common.Helper;

    using Helpers;

    using Message;
    using Message.Helpers;

    using Microsoft.Win32;

    using MimeKit;

    using Serilog;

    public class MessageDetailPartsListViewModel : Screen, IMessageDetailItem
    {
        readonly ILogger _logger;

        private readonly MessageRepository _messageRepository;
        readonly IViewModelWindowManager _viewModelWindowManager;
        bool _hasSelectedPart;

        MimeMessage _mimeMessage;

        MimeEntity _selectedPart;

        public MessageDetailPartsListViewModel(MessageRepository messageRepository, IViewModelWindowManager viewModelWindowManager, ILogger logger)
        {
            DisplayName = "Sections";
            _messageRepository = messageRepository;
            _viewModelWindowManager = viewModelWindowManager;
            _logger = logger;
            Parts = new ObservableCollection<MimeEntity>();
        }

        public ObservableCollection<MimeEntity> Parts { get; }

        public MimeMessage MimeMessage
        {
            get => _mimeMessage;
            set
            {
                _mimeMessage = value;
                NotifyOfPropertyChange(() => MimeMessage);

                if (_mimeMessage != null)
                    RefreshParts();
            }
        }

        public MimeEntity SelectedPart
        {
            get => _selectedPart;
            set
            {
                _selectedPart = value;
                HasSelectedPart = _selectedPart != null;
                NotifyOfPropertyChange(() => SelectedPart);
            }
        }

        public bool HasSelectedPart
        {
            get => _hasSelectedPart;
            set
            {
                _hasSelectedPart = value;
                NotifyOfPropertyChange(() => HasSelectedPart);
            }
        }

        public void ViewSection()
        {
            MimeEntity part = SelectedPart;

            if (part == null)
            {
                return;
            }

            if (part is TextPart textPart)
            {
                // show in the viewer...
                _viewModelWindowManager.ShowDialogWithViewModel<MimePartViewModel>(vm => vm.PartText = textPart.Text);
            }
            else if (part is MessagePart messagePart)
            {
                _viewModelWindowManager.ShowDialogWithViewModel<MimePartViewModel>(vm => vm.PartText = messagePart.Message.ToString());
            }
            else if (part is MimePart mimePart)
            {
                string tempFileName;

                if (mimePart.FileName.IsSet())
                    tempFileName = GeneralExtensions.GetOriginalFileName(Path.GetTempPath(), mimePart.FileName);
                else
                {
                    tempFileName = Path.GetTempFileName();
                    string extension = part.ContentType.GetExtension();

                    if (extension.IsSet())
                        tempFileName = Path.ChangeExtension(tempFileName, extension);
                }

                try
                {
                    using (FileStream outputFile = File.Open(tempFileName, FileMode.Create))
                    {
                        mimePart.Content.DecodeTo(outputFile);
                    }

                    Process.Start(tempFileName);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failure Creating and Opening Up Attachment File: {TempFileName}", tempFileName);
                    MessageBox.Show($"Failed to Open Attachment File: {ex.Message}",
                        "Unable to Open Attachment");
                }
            }
        }

        public void SaveAs()
        {
            if (SelectedPart is MimePart mimePart)
            {
                var dlg = new SaveFileDialog();
                if (!string.IsNullOrWhiteSpace(mimePart.FileName))
                    dlg.FileName = mimePart.FileName;

                var extensions = new List<string>();
                if (mimePart.ContentType.MediaSubtype != "Unknown")
                {
                    string extension = mimePart.ContentType.GetExtension();

                    if (!string.IsNullOrWhiteSpace(extension))
                    {
                        extensions.Add(string.Format("{0} (*{1})|*{1}",
                            Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(mimePart.ContentType.MediaSubtype),
                            extension));
                    }
                }

                extensions.Add("All (*.*)|*.*");
                dlg.Filter = string.Join("|", extensions.ToArray());

                bool? result = dlg.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    _logger.Debug("Saving File {File} as Output for MimePart {PartFileName}", dlg.FileName,
                        mimePart.FileName);

                    // save it..
                    using (Stream outputFile = dlg.OpenFile())
                    {
                        mimePart.Content.DecodeTo(outputFile);
                    }
                }
            }
            else if (SelectedPart is MessagePart messagePart)
            {
                var dlg = new SaveFileDialog();

                var fileName = _messageRepository.GetFullMailFilename(messagePart.Message.Subject);

                dlg.FileName = Path.GetFileName(fileName);
                dlg.InitialDirectory = Path.GetDirectoryName(fileName);

                var extensions = new List<string> {"Email (*.eml)|*.eml", "All (*.*)|*.*"};

                dlg.Filter = string.Join("|", extensions.ToArray());

                bool? result = dlg.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    _logger.Debug("Saved Embedded Message as {MessageFile}", dlg.FileName);

                    // save it..
                    using (Stream outputFile = dlg.OpenFile())
                    {
                        messagePart.Message.WriteTo(outputFile);
                    }
                }
            }
        }

        void RefreshParts()
        {
            Parts.Clear();
            Parts.AddRange(MimeMessage.BodyParts);
        }
    }
}