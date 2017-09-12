using MimeKit;
using System.IO;
using System.Net.Http;

namespace Papercut.Module.WebUI.Helpers
{
    public class MimePartResponseMessage : HttpResponseMessage
    {
        readonly string tempFilePath;

        public MimePartResponseMessage(HttpRequestMessage requestMessage, IContentObject contentObject)
        {
            tempFilePath = Path.GetTempFileName();

            RequestMessage = requestMessage;

            using (var tempFile = File.OpenWrite(tempFilePath))
            {
                contentObject.DecodeTo(tempFile);
            }
            Content = new StreamContent(File.OpenRead(tempFilePath));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Content.Dispose();

            try
            {
                File.Delete(tempFilePath);
            }
            catch
            {
                // ignored
            }
        }
    }
}
