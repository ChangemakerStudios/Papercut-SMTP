namespace Papercut.Domain.HtmlPreviews
{
    using MimeKit;

    public interface IHtmlPreviewGenerator
    {
        string GetHtmlPreview(MimeMessage mailMessageEx, string tempDir = null);

        string GetHtmlPreviewFile(MimeMessage mailMessageEx, string tempDir = null);
    }
}