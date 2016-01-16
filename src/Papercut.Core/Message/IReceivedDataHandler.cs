namespace Papercut.Core.Message
{
    using System.Collections.Generic;

    public interface IReceivedDataHandler
    {
        void HandleReceived(string message, IList<string> recipients);
    }
}