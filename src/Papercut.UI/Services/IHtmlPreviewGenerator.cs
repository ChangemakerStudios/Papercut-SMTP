namespace Papercut.Services
{
    using MimeKit;

    public interface IHtmlPreviewGenerator
    {
        string GetHtmlPreview(MimeMessage mailMessageEx);
    }
}