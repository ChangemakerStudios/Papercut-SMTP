namespace Papercut.Module.WebUI.Models
{
    public class MessageDetail
    {

        public string Subject { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Cc { get; set; }
        public string Bcc { get; set; }

        public string BodyContent { get; set; }
        public bool IsBodyHtml { get; set; }

    }
}