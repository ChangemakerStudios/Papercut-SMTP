namespace Papercut.Domain.HtmlPreviews
{
    using MimeKit;

    public interface IHtmlPreviewGenerator
    {
        string CreateFile(MimeMessage mailMessageEx);
    }
}