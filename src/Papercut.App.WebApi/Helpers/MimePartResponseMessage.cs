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

namespace Papercut.App.WebApi.Helpers
{
    using System.IO;
    using System.Net.Http;

    using MimeKit;

    public class MimePartResponseMessage : HttpResponseMessage
    {
        readonly string _tempFilePath;

        public MimePartResponseMessage(HttpRequestMessage requestMessage, IMimeContent contentObject)
        {
            _tempFilePath = Path.GetTempFileName();

            RequestMessage = requestMessage;

            using (var tempFile = File.OpenWrite(_tempFilePath))
            {
                contentObject.DecodeTo(tempFile);
            }
            Content = new StreamContent(File.OpenRead(_tempFilePath));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Content.Dispose();

            try
            {
                File.Delete(_tempFilePath);
            }
            catch
            {
                // ignored
            }
        }
    }
}
