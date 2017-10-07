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

namespace Papercut.Service.Web
{
    using Microsoft.AspNetCore.Mvc;
    using MimeKit;
    using System.IO;
    using System.Threading.Tasks;


    public class MimePartFileStreamResult : FileStreamResult
    {
        readonly string tempFilePath;

        public MimePartFileStreamResult(IContentObject contentObject, string contentType) : base(new MemoryStream(), contentType)
        {
            tempFilePath = Path.GetTempFileName();

            using (var tempFile = File.OpenWrite(tempFilePath))
            {
                contentObject.DecodeTo(tempFile);
            }

            this.FileStream.Dispose();
            this.FileStream = File.OpenRead(tempFilePath);
        }

        public override void ExecuteResult(ActionContext context)
        {
            base.ExecuteResult(context);
            Cleanup();
        }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            return base.ExecuteResultAsync(context).ContinueWith((task) => {
                Cleanup();
            });
        }


        void Cleanup()
        {
            try
            {
                File.Delete(tempFilePath);
            }
            catch
            {
                // ignore errors
            }
        }
    }
}
