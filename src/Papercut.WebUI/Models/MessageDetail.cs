namespace Papercut.WebUI.Models
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



        /* HeaderViewModel.Headers = string.Join("\r\n", mailMessageEx.Headers.Select(h => h.ToString()));

        var parts = mailMessageEx.BodyParts.OfType<MimePart>().ToList();
        var mainBody = parts.GetMainBodyTextPart();

        From = mailMessageEx.From?.ToString() ?? string.Empty;
        To = mailMessageEx.To?.ToString() ?? string.Empty;
        CC = mailMessageEx.Cc?.ToString() ?? string.Empty;
        Bcc = mailMessageEx.Bcc?.ToString() ?? string.Empty;
        Date = mailMessageEx.Date.ToString();
        Subject = mailMessageEx.Subject ?? string.Empty;

        AttachmentCount = parts.GetAttachments().Count();
        RawViewModel.MimeMessage = mailMessageEx;
        PartsListViewModel.MimeMessage = mailMessageEx;

        BodyViewModel.Body = mainBody != null ? mainBody.GetText(Encoding.UTF8) : string.Empty;

        if (mainBody != null) {
            IsHtml = mainBody.IsContentHtml();
            HtmlViewModel.ShowMessage(mailMessageEx);

            if (IsHtml)
            {
                var textPartNotHtml = parts.OfType<TextPart>().Except(new[] { mainBody }).FirstOrDefault();
                if (textPartNotHtml != null)
                    TextBody = textPartNotHtml.GetText(Encoding.UTF8);
            }
        }


    */
    }
}