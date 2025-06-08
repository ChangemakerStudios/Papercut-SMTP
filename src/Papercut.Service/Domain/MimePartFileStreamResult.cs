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

namespace Papercut.Service.Domain;

public class MimePartFileStreamResult : FileStreamResult
{
    private readonly string _tempFilePath;

    public MimePartFileStreamResult(IMimeContent contentObject, string contentType)
        : base(new MemoryStream(), contentType)
    {
        _tempFilePath = Path.GetTempFileName();

        using (var tempFile = File.OpenWrite(_tempFilePath))
        {
            contentObject.DecodeTo(tempFile);
        }

        FileStream.Dispose();
        FileStream = File.OpenRead(_tempFilePath);
    }

    public override void ExecuteResult(ActionContext context)
    {
        base.ExecuteResult(context);
        Cleanup();
    }

    public override Task ExecuteResultAsync(ActionContext context)
    {
        return base.ExecuteResultAsync(context).ContinueWith(
            _ =>
            {
                Cleanup();
            });
    }

    private void Cleanup()
    {
        try
        {
            File.Delete(_tempFilePath);
        }
        catch
        {
            // ignore errors
        }
    }
}