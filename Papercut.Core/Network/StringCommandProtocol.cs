namespace Papercut.Core.Network
{
    using System;
    using System.Text;

    using Serilog;

    public abstract class StringCommandProtocol : IProtocol
    {
        StringBuilder _stringBuffer = new StringBuilder();

        protected StringCommandProtocol(ILogger logger)
        {
            Logger = logger;
        }

        public ILogger Logger { get; set; }

        public abstract void Begin(Connection connection);

        public virtual void ProcessIncomingBuffer(byte[] bufferedData)
        {
            // Get the string data and append to buffer
            string data = Encoding.ASCII.GetString(bufferedData, 0, bufferedData.Length);

            _stringBuffer.Append(data);

            // Check if the string buffer contains a line break
            string line = _stringBuffer.ToString().Replace("\r", string.Empty);

            while (line.Contains("\n"))
            {
                // Take a snippet of the buffer, find the line, and process it
                _stringBuffer =
                    new StringBuilder(
                        line.Substring(line.IndexOf("\n", StringComparison.Ordinal) + 1));

                line = line.Substring(0, line.IndexOf("\n", StringComparison.Ordinal));

                Logger.Debug("Received Line {Line}", line);

                ProcessCommand(line);

                line = _stringBuffer.ToString();
            }
        }

        protected abstract void ProcessCommand(string command);
    }
}