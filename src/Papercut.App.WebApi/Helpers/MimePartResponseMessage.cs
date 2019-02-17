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
            this._tempFilePath = Path.GetTempFileName();

            this.RequestMessage = requestMessage;

            using (var tempFile = File.OpenWrite(this._tempFilePath))
            {
                contentObject.DecodeTo(tempFile);
            }
            this.Content = new StreamContent(File.OpenRead(this._tempFilePath));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this.Content.Dispose();

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
