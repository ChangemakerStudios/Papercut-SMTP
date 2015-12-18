namespace Papercut.Services
{
    using MimeKit;

    public interface IHtmlPreviewGenerator
    {
        string CreateFile(MimeMessage mailMessageEx);
    }
}