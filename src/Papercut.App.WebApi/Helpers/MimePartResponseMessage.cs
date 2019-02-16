using MimeKit;
using System.IO;
using System.Net.Http;

namespace Papercut.Module.WebUI.Helpers
{
    public class MimePartResponseMessage : HttpResponseMessage
    {
        readonly string _tempFilePath;

        public MimePartResponseMessage(HttpRequestMessage requestMessage, IMimeContent contentObject)
        {
            this._tempFilePath = Path.GetTempFileName();

            RequestMessage = requestMessage;

            using (var tempFile = File.OpenWrite(this._tempFilePath))
            {
                contentObject.DecodeTo(tempFile);
            }
            Content = new StreamContent(File.OpenRead(this._tempFilePath));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Content.Dispose();

            try
            {
                File.Delete(this._tempFilePath);
            }
            catch
            {
                // ignored
            }
        }
    }
}
