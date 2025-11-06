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


namespace Papercut.AppLayer.Attachments;

using Papercut.Message.Helpers;

public class AttachmentFileService()
{
    public string CreateTempFileForAttachment(MimePart mimePart)
    {
        string tempFileName;

        if (mimePart.FileName.IsSet())
        {
            string? originalFileName = GeneralExtensions.GetOriginalFileName(Path.GetTempPath(), mimePart.FileName);

            if (originalFileName != null)
            {
                tempFileName = originalFileName;
            }
            else
            {
                // Fall back to temp file with extension if GetOriginalFileName returns null
                tempFileName = CreateTempFileWithExtension(mimePart.ContentType.GetExtension());
            }
        }
        else
        {
            tempFileName = CreateTempFileWithExtension(mimePart.ContentType.GetExtension());
        }

        return tempFileName;
    }

    public void WriteAttachmentToFile(MimePart mimePart, string filePath)
    {
        using FileStream outputFile = File.Open(filePath, FileMode.Create);
        mimePart.Content.DecodeTo(outputFile);
    }

    private string CreateTempFileWithExtension(string? extension)
    {
        string tempFileName = Path.GetTempFileName();

        if (extension.IsSet())
        {
            tempFileName = Path.ChangeExtension(tempFileName, extension);
        }

        return tempFileName;
    }

    #region Begin Static Container Registrations

    private static void Register(ContainerBuilder builder)
    {
        builder.RegisterType<AttachmentFileService>().AsSelf();
    }

    #endregion
}
