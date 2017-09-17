using Microsoft.AspNetCore.Mvc;
using MimeKit;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Papercut.Module.WebUI.Helpers
{
    public class MimePartFileStreamResult : FileStreamResult
    {
        readonly string tempFilePath;
        private MediaTypeHeaderValue mediaTypeHeaderValue;

        public MimePartFileStreamResult(IContentObject contentObject, string contentType) : base(null, contentType)
        {
            tempFilePath = Path.GetTempFileName();

            using (var tempFile = File.OpenWrite(tempFilePath))
            {
                contentObject.DecodeTo(tempFile);
            }

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
