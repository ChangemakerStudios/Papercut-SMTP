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

namespace Papercut.Common.Extensions
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Papercut.Core.Annotations;

    public static class StreamExtensions
    {
        public static async Task<string> ReadString(
            this Stream stream,
            int bufferSize = 0xFF0,
            Encoding encoding = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var serverbuff = new byte[bufferSize];

            encoding = encoding ?? Encoding.UTF8;

            int count = await stream.ReadAsync(serverbuff, 0, bufferSize);

            return count == 0 ? string.Empty : encoding.GetString(serverbuff, 0, count);
        }

        public static async Task WriteFormat(this Stream stream, string format, params object[] args)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            await stream.WriteStr(string.Format(format, args));
        }

        public static async Task WriteLine(this Stream stream, string str, Encoding encoding = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            encoding = encoding ?? Encoding.UTF8;

            await stream.WriteBytes(encoding.GetBytes($"{str}\r\n"));
        }

        public static async Task WriteStr(this Stream stream, string str, Encoding encoding = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            encoding = encoding ?? Encoding.UTF8;

            await stream.WriteBytes(encoding.GetBytes(str));
        }

        public static async Task WriteBytes(this Stream stream, byte[] data)
        {
            await stream.WriteAsync(data, 0, data.Length);
        }

        public static async Task<Stream> CopyBufferedTo(
            this Stream source,
            Stream destination,
            int bufferLength = 0xFFF)
        {
            var buffer = new byte[bufferLength];
            int bytesRead = await source.ReadAsync(buffer, 0, bufferLength);

            // write the required bytes
            while (bytesRead > 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead);
                bytesRead = await source.ReadAsync(buffer, 0, bufferLength);
            }

            return source;
        }

        public static Stream CopyBufferedLimited(
            this Stream source,
            Stream destination,
            int size,
            int bufferLength = 0xFFF)
        {
            var buffer = new byte[bufferLength];

            int bytesRead;

            for (int readCount = 0; readCount < size; readCount += bytesRead)
            {
                bytesRead = source.Read(buffer, 0, buffer.Length);

                if (bytesRead == 0) break;

                destination.Write(buffer, 0, bytesRead);

            }

            return source;
        }
    }
}