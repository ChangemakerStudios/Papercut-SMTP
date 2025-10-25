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


using System.Collections.ObjectModel;

using Papercut.Message;
using Papercut.Message.Helpers;

namespace Papercut.ViewModels;

public sealed class MessageDetailAttachmentsViewModel : PropertyChangedBase
{
    readonly ILogger _logger;

    public MessageDetailAttachmentsViewModel(ILogger logger)
    {
        this._logger = logger;
        this.Attachments = new ObservableCollection<MimePart>();
    }

    public ObservableCollection<MimePart> Attachments { get; }

    public bool HasAttachments => this.Attachments.Count > 0;

    public void LoadAttachments(MimeMessage? mimeMessage)
    {
        this.Attachments.Clear();

        if (mimeMessage != null)
        {
            var parts = mimeMessage.BodyParts.OfType<MimePart>().ToList();
            var attachments = parts.GetAttachments();
            this.Attachments.AddRange(attachments);
        }

        this.NotifyOfPropertyChange(() => this.HasAttachments);
    }

    public void OpenAttachment(MimePart? mimePart)
    {
        if (mimePart == null)
        {
            return;
        }
        string tempFileName;

        if (mimePart.FileName.IsSet())
        {
            tempFileName = GeneralExtensions.GetOriginalFileName(Path.GetTempPath(), mimePart.FileName);
        }
        else
        {
            tempFileName = Path.GetTempFileName();
            string extension = mimePart.ContentType.GetExtension();

            if (extension.IsSet())
                tempFileName = Path.ChangeExtension(tempFileName, extension);
        }

        try
        {
            using (FileStream outputFile = File.Open(tempFileName, FileMode.Create))
            {
                mimePart.Content.DecodeTo(outputFile);
            }

            // Set WorkingDirectory to file's directory to avoid path resolution issues on Windows 11
            // Explicitly set Verb to "open" for reliability with shell file associations
            var directory = Path.GetDirectoryName(tempFileName) ?? Path.GetTempPath();
            var processStartInfo = new ProcessStartInfo(tempFileName)
            {
                UseShellExecute = true,
                WorkingDirectory = directory,
                Verb = "open"
            };

            Process.Start(processStartInfo);
        }
        catch (Exception ex)
        {
            this._logger.Error(ex, "Failure Creating and Opening Up Attachment File: {TempFileName}", tempFileName);
            MessageBox.Show($"Failed to Open Attachment File: {ex.Message}",
                "Unable to Open Attachment");
        }
    }
}
