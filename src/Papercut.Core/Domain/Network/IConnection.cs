namespace Papercut.Core.Domain.Network
{
    using System;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;

    using Serilog;

    public interface IConnection
    {
        event EventHandler ConnectionClosed;

        IProtocol Protocol { get; }

        ILogger Logger { get; }

        Socket Client { get; }

        bool Connected { get; }

        int Id { get; }

        DateTime LastActivity { get; set; }

        void Close(bool triggerEvent = true);

        Encoding Encoding { get; set; }

        Task SendData(byte[] data);
    }
}