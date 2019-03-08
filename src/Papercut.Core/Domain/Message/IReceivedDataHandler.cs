namespace Papercut.Core.Domain.Message
{
    public interface IReceivedDataHandler
    {
        void HandleReceived(byte[] message, string[] recipients);
    }
}