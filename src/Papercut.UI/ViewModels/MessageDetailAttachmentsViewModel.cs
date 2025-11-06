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

using Papercut.AppLayer.Attachments;
using Papercut.AppLayer.Processes;
using Papercut.Domain.UiCommands;
using Papercut.Message;
using Papercut.Message.Helpers;

namespace Papercut.ViewModels;

public sealed class MessageDetailAttachmentsViewModel : PropertyChangedBase
{
    readonly ILogger _logger;

    readonly AttachmentFileService _attachmentFileService;

    readonly ProcessService _processService;

    readonly IUiCommandHub _uiCommandHub;

    public MessageDetailAttachmentsViewModel(
        ILogger logger,
        AttachmentFileService attachmentFileService,
        ProcessService processService,
        IUiCommandHub uiCommandHub)
    {
        this._logger = logger;
        this._attachmentFileService = attachmentFileService;
        this._processService = processService;
        this._uiCommandHub = uiCommandHub;
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

        try
        {
            string tempFileName = this._attachmentFileService.CreateTempFileForAttachment(mimePart);
            this._attachmentFileService.WriteAttachmentToFile(mimePart, tempFileName);

            var result = this._processService.OpenFile(tempFileName);

            if (result.IsFailed)
            {
                this._uiCommandHub.ShowMessage(
                    string.Join(Environment.NewLine, result.Errors),
                    "Unable to Open Attachment");
            }
        }
        catch (Exception ex)
        {
            this._logger.Error(ex, "Failure Creating Attachment File for {FileName}", mimePart.FileName);
            this._uiCommandHub.ShowMessage(
                $"Failed to Create Attachment File: {ex.Message}",
                "Unable to Create Attachment");
        }
    }
}
