namespace Papercut.Core.Domain.Message
{
    using System.Text;

    public interface IReceivedDataHandler
    {
        void HandleReceived(string message, string[] recipients, Encoding connectionEncoding);
    }
}