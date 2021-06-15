namespace Papercut.Core.Domain.Message
{
    using System.Threading.Tasks;

    public interface IReceivedDataHandler
    {
        Task HandleReceivedAsync(byte[] message, string[] recipients);
    }
}