// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2017 Jaben Cargman
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

    using Microsoft.Win32;

    using MimeKit;

    using Papercut.Common.Extensions;
    using Papercut.Common.Helper;
    using Papercut.Helpers;
    using Papercut.Message.Helpers;

    using Serilog;

    public class MessageDetailPartsListViewModel : Screen, IMessageDetailItem
    {
        bool _hasSelectedPart;

        MimeMessage _mimeMessage;

        MimePart _selectedPart;

        readonly ILogger _logger;

        readonly IViewModelWindowManager _viewModelWindowManager;

        public MessageDetailPartsListViewModel(IViewModelWindowManager viewModelWindowManager, ILogger logger)
        {
            DisplayName = "Sections";
            _viewModelWindowManager = viewModelWindowManager;
            _logger = logger;
            Parts = new ObservableCollection<MimePart>();
        }

        public ObservableCollection<MimePart> Parts { get; private set; }

        public MimeMessage MimeMessage
        {
            get { return _mimeMessage; }
            set
            {
                _mimeMessage = value;
                NotifyOfPropertyChange(() => MimeMessage);

                if (_mimeMessage != null)
                    RefreshParts();
            }
        }

        public MimePart SelectedPart
        {
            get { return _selectedPart; }
            set
            {
                _selectedPart = value;
                HasSelectedPart = _selectedPart != null;
                NotifyOfPropertyChange(() => SelectedPart);
            }
        }

        public bool HasSelectedPart
        {
            get { return _hasSelectedPart; }
            set
            {
                _hasSelectedPart = value;
                NotifyOfPropertyChange(() => HasSelectedPart);
            }
        }

        public void ViewSection()
        {
            MimePart part = SelectedPart;

            if (part == null)
            {
                return;
            }

            if (part is TextPart)
            {
                var textPart = part as TextPart;

                // show in the viewer...
                _viewModelWindowManager.ShowDialogWithViewModel<MimePartViewModel>(vm => vm.PartText = textPart.Text);
            }
            else
            {
                string tempFileName;

                if (part.FileName.IsSet())
                    tempFileName = GeneralExtensions.GetOriginalFileName(Path.GetTempPath(), part.FileName);
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
                        part.Content.DecodeTo(outputFile);
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
            MimePart part = SelectedPart;

            var dlg = new SaveFileDialog();
            if (!string.IsNullOrWhiteSpace(part.FileName))
                dlg.FileName = part.FileName;

            var extensions = new List<string>();
            if (part.ContentType.MediaSubtype != "Unknown")
            {
                string extension = part.ContentType.GetExtension();

                if (!string.IsNullOrWhiteSpace(extension))
                {
                    extensions.Add(string.Format("{0} (*{1})|*{1}",
                        Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(part.ContentType.MediaSubtype),
                        extension));
                }
            }

            extensions.Add("All (*.*)|*.*");
            dlg.Filter = string.Join("|", extensions.ToArray());

            bool? result = dlg.ShowDialog();

            if (result.HasValue && result.Value)
            {
                _logger.Debug("Saving File {File} as Output for MimePart {PartFileName}", dlg.FileName, part.FileName);

                // save it..
                using (Stream outputFile = dlg.OpenFile())
                {
                    part.Content.DecodeTo(outputFile);
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