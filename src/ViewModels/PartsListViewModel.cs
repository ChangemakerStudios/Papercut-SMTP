/*  
 * Papercut
 *
 *  Copyright © 2008 - 2012 Ken Robertson
 *  Copyright © 2013 - 2014 Jaben Cargman
 *  
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *  
 *  http://www.apache.org/licenses/LICENSE-2.0
 *  
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *  
 */

namespace Papercut.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Windows.Documents;

    using Caliburn.Micro;

    using MimeKit;

    using Papercut.Core.Helper;

    using Serilog;

    public class PartsListViewModel : Screen
    {
        readonly IWindowManager _windowManager;

        readonly ILogger _logger;

        public PartsListViewModel(Func<MimePartViewModel> mimePartViewFactory, IWindowManager windowManager, ILogger logger)
        {
            _windowManager = windowManager;
            _logger = logger;
            Parts = new ObservableCollection<MimePart>();
            _mimePartViewModel = mimePartViewFactory();
        }

        MimeMessage _mimeMessage;

        MimePart _selectedPart;

        readonly MimePartViewModel _mimePartViewModel;

        bool _hasSelectedPart;

        public ObservableCollection<MimePart> Parts { get; private set; }

        public MimeMessage MimeMessage
        {
            get
            {
                return _mimeMessage;
            }
            set
            {
                _mimeMessage = value;
                NotifyOfPropertyChange(() => MimeMessage);

                if (_mimeMessage != null)
                {
                    RefreshParts();
                }
            }
        }

        public MimePart SelectedPart
        {
            get
            {
                return _selectedPart;
            }
            set
            {
                _selectedPart = value;
                HasSelectedPart = _selectedPart != null;
                NotifyOfPropertyChange(() => SelectedPart);
            }
        }

        public bool HasSelectedPart
        {
            get
            {
                return _hasSelectedPart;
            }
            set
            {
                _hasSelectedPart = value;
                NotifyOfPropertyChange(() => HasSelectedPart);
            }
        }

        public void ViewSection()
        {
            var part = SelectedPart;

            if (part is TextPart)
            {
                var textPart = part as TextPart;

                // show in the viewer...
                _mimePartViewModel.PartText = textPart.Text;
                _windowManager.ShowDialog(_mimePartViewModel);
            }
            else
            {
                string tempFileName;

                if (!string.IsNullOrWhiteSpace(part.FileName))
                {
                    tempFileName = Path.Combine(Path.GetTempPath(), part.FileName);
                }
                else
                {
                    tempFileName = Path.GetTempFileName();

                    string extension = part.ContentType.GetExtension();

                    if (!string.IsNullOrWhiteSpace(extension))
                    {
                        tempFileName = Path.ChangeExtension(tempFileName, extension);
                    }
                }

                using (var outputFile = File.Open(tempFileName, FileMode.Create))
                {
                    part.ContentObject.DecodeTo(outputFile);
                    outputFile.Close();
                }

                Process.Start(tempFileName);
            }
        }

        public void SaveAs()
        {
            var part = SelectedPart;

            var dlg = new Microsoft.Win32.SaveFileDialog();
            if (!string.IsNullOrWhiteSpace(part.FileName))
            {
                dlg.FileName = part.FileName;
            }

            var extensions = new List<string>();
            if (part.ContentType.MediaSubtype != "Unknown")
            {
                var extension = part.ContentType.GetExtension();

                if (!string.IsNullOrWhiteSpace(extension))
                {
                    extensions.Add(
                        string.Format(
                            "{0} (*{1})|*{1}",
                            Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(
                                part.ContentType.MediaSubtype),
                            extension));
                }
            }

            extensions.Add("All (*.*)|*.*");
            dlg.Filter = string.Join("|", extensions.ToArray());

            var result = dlg.ShowDialog();

            if (result.HasValue && result.Value)
            {
                _logger.Debug(
                    "Saving File {File} as Output for MimePart {PartFileName}",
                    dlg.FileName,
                    part.FileName);

                // save it..
                using (var outputFile = dlg.OpenFile())
                {
                    part.ContentObject.DecodeTo(outputFile);
                    outputFile.Close();
                }
            }
        }

        void RefreshParts()
        {
            Parts.Clear();
            Parts.AddRange(MimeMessage.BodyParts.ToList());
        }
    }
}