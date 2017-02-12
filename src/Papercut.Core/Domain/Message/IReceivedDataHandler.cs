namespace Papercut.Core.Message
{
    using System.Collections.Generic;
    using System.Text;

    public interface IReceivedDataHandler
    {
        void HandleReceived(string message, string[] recipients, Encoding connectionEncoding);
    }
}