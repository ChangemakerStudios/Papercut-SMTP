// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2024 Jaben Cargman
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


using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

using Caliburn.Micro;

using Microsoft.Win32;

using MimeKit;

using Papercut.Common.Extensions;
using Papercut.Common.Helper;
using Papercut.Helpers;
using Papercut.Message;
using Papercut.Message.Helpers;

namespace Papercut.ViewModels;

public sealed class MessageDetailPartsListViewModel : Screen, IMessageDetailItem
{
    readonly ILogger _logger;

    private readonly IMessageRepository _messageRepository;

    readonly IViewModelWindowManager _viewModelWindowManager;

    bool _hasSelectedPart;

    MimeMessage? _mimeMessage;

    MimeEntity? _selectedPart;

    public MessageDetailPartsListViewModel(IMessageRepository messageRepository, IViewModelWindowManager viewModelWindowManager, ILogger logger)
    {
        this.DisplayName = "Sections";
        this._messageRepository = messageRepository;
        this._viewModelWindowManager = viewModelWindowManager;
        this._logger = logger;
        this.Parts = new ObservableCollection<MimeEntity>();
    }

    public ObservableCollection<MimeEntity> Parts { get; }

    public MimeMessage? MimeMessage
    {
        get => this._mimeMessage;
        set
        {
            this._mimeMessage = value;
            this.NotifyOfPropertyChange(() => this.MimeMessage);

            if (this._mimeMessage != null) this.RefreshParts();
        }
    }

    public MimeEntity? SelectedPart
    {
        get => this._selectedPart;
        set
        {
            this._selectedPart = value;
            this.HasSelectedPart = this._selectedPart != null;
            this.NotifyOfPropertyChange(() => this.SelectedPart);
        }
    }

    public bool HasSelectedPart
    {
        get => this._hasSelectedPart;
        set
        {
            this._hasSelectedPart = value;
            this.NotifyOfPropertyChange(() => this.HasSelectedPart);
        }
    }

    public void ViewSection()
    {
        MimeEntity part = this.SelectedPart;

        if (part == null)
        {
            return;
        }

        if (part is TextPart textPart)
        {
            // show in the viewer...
            this._viewModelWindowManager.ShowDialogWithViewModel<MimePartViewModel>(vm => vm.PartText = textPart.Text);
        }
        else if (part is MessagePart messagePart)
        {
            this._viewModelWindowManager.ShowDialogWithViewModel<MimePartViewModel>(vm => vm.PartText = messagePart.Message.ToString());
        }
        else if (part is MimePart mimePart)
        {
            string tempFileName;

            if (mimePart.FileName.IsSet())
            {
                tempFileName = GeneralExtensions.GetOriginalFileName(Path.GetTempPath(), mimePart.FileName);
            }
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

                Process.Start(new ProcessStartInfo(tempFileName));
            }
            catch (Exception ex)
            {
                this._logger.Error(ex, "Failure Creating and Opening Up Attachment File: {TempFileName}", tempFileName);
                MessageBox.Show($"Failed to Open Attachment File: {ex.Message}",
                    "Unable to Open Attachment");
            }
        }
    }

    public void SaveAs()
    {
        if (this.SelectedPart is MimePart mimePart)
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
                this._logger.Debug("Saving File {File} as Output for MimePart {PartFileName}", dlg.FileName,
                    mimePart.FileName);

                // save it…
                using Stream outputFile = dlg.OpenFile();

                mimePart.Content.DecodeTo(outputFile);
            }
        }
        else if (this.SelectedPart is MessagePart messagePart)
        {
            var dlg = new SaveFileDialog();

            var fileName = this._messageRepository.GetFullMailFilename(messagePart.Message.Subject);

            dlg.FileName = Path.GetFileName(fileName);
            dlg.InitialDirectory = Path.GetDirectoryName(fileName);

            var extensions = new List<string> {"Email (*.eml)|*.eml", "All (*.*)|*.*"};

            dlg.Filter = string.Join("|", extensions);

            bool? result = dlg.ShowDialog();

            if (result.HasValue && result.Value)
            {
                this._logger.Debug("Saved Embedded Message as {MessageFile}", dlg.FileName);

                // save it…
                using Stream outputFile = dlg.OpenFile();

                messagePart.Message.WriteTo(outputFile);
            }
        }
    }

    void RefreshParts()
    {
        this.Parts.Clear();
        this.Parts.AddRange(this.MimeMessage.BodyParts);
    }
}