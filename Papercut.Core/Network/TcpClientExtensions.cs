namespace Papercut.Core.Network
{
    using System;
    using System.Net.Sockets;
    using System.Text;

    public static class TcpClientExtensions
    {
        public static string ReadString(this TcpClient client)
        {
            if (client == null) throw new ArgumentNullException("client");

            const int BufferSize = 1024;
            var serverbuff = new byte[BufferSize];

            int count = client.GetStream().Read(serverbuff, 0, BufferSize);
            return count == 0 ? string.Empty : new ASCIIEncoding().GetString(serverbuff, 0, count);
        }

        public static void WriteFormat(this TcpClient client, string format, params object[] args)
        {
            if (client == null) throw new ArgumentNullException("client");

            client.WriteString(string.Format(format, args));
        }

        public static void WriteString(this TcpClient client, string str)
        {
            if (client == null) throw new ArgumentNullException("client");

            client.WriteBytes(new ASCIIEncoding().GetBytes(str));
        }

        public static void WriteBytes(this TcpClient client, byte[] data)
        {
            client.GetStream().Write(data, 0, data.Length);
        }
    }
}