namespace Papercut.Core.Message
{
    using System.Collections.Generic;

    public interface IReceivedDataHandler
    {
        void HandleReceived(IEnumerable<string> data);
    }
}