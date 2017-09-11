using MimeKit;
using System.IO;
using System.Net.Http;

namespace Papercut.Module.WebUI.Helpers
{
    public class MimePartResponseMessage : HttpResponseMessage
    {
        private IContentObject _contentObject;
        private string _tempFilePath;

        public MimePartResponseMessage(HttpRequestMessage requestMessage, IContentObject contentObject)
        {
            RequestMessage = requestMessage;
            this._contentObject = contentObject;
            _tempFilePath = Path.GetTempFileName();

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
            catch { }

        }
    }
}
