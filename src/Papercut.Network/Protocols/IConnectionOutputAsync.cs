namespace Papercut.Network.Protocols
{
    using System.Threading.Tasks;

    public interface IConnectionOutputAsync
    {
        Task<int> SendLine(string message);

        Task<int> SendLine(
            string message,
            params object[] args);

        Task<int> Send(
            string message,
            params object[] args);

        Task<int> SendBytes(byte[] data);
    }
}