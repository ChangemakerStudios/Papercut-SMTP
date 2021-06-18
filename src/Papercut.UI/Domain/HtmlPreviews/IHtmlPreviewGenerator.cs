namespace Papercut.Domain.HtmlPreviews
{
    using MimeKit;

    public interface IHtmlPreviewGenerator
    {
        string GetHtmlPreview(MimeMessage mailMessageEx);
    }
}