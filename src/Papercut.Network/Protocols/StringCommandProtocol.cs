// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2017 Jaben Cargman
//  
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//  
// http://www.apache.org/licenses/LICENSE-2.0
//  
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License. 


namespace Papercut.Network.Protocols
{
    using System;
    using System.Text;

    using Papercut.Core.Domain.Network;

    using Serilog;

    public abstract class StringCommandProtocol : IProtocol
    {
        StringBuilder _stringBuffer = new StringBuilder();

        protected StringCommandProtocol(ILogger logger)
        {
            _logger = logger;
        }

        protected ILogger _logger { get; set; }

        public abstract void Begin(IConnection connection);

        public virtual void ProcessIncomingBuffer(byte[] bufferedData, Encoding encoding)
        {
            // Get the string data and append to buffer
            string data = encoding.GetString(bufferedData, 0, bufferedData.Length);

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

                _logger.Debug("Received Line {Line}", line);

                ProcessRequest(line);

                line = _stringBuffer.ToString();
            }
        }

        protected abstract void ProcessRequest(string request);
    }
}