// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2021 Jaben Cargman
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


namespace Papercut.Infrastructure.IPComm
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;

    using Papercut.Infrastructure.IPComm.Network;

    public static class StreamExtensions
    {
        public static async Task<byte[]> ReadBufferedAsync(
            this NetworkStream stream,
            int? maxBytes = null,
            int bufferSize = 0xFFF)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            using (var ms = new MemoryStream())
            {
                await stream.CopyBufferedToAsync(ms, maxBytes, bufferSize);

                return ms.ToArray();
            }
        }

        public static async Task<string> ReadStringBufferedAsync(
            this NetworkStream stream,
            int? maxBytes = null,
            int bufferSize = 0xFFF,
            Encoding encoding = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            encoding = encoding ?? Encoding.UTF8;

            var readBufferedAsync = await stream.ReadBufferedAsync(maxBytes, bufferSize);

            return encoding.GetString(readBufferedAsync);
        }

        public static async Task<object> ReadJsonBufferedAsync(
            this NetworkStream stream,
            Type type,
            int? maxBytes = null,
            int bufferSize = 0xFFF, Encoding encoding = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var readStringBufferedAsync = await stream.ReadStringBufferedAsync(maxBytes, bufferSize, encoding);

            return PapercutIPCommSerializer.FromJson(type, readStringBufferedAsync);
        }

        public static async Task WriteFormatAsync(this Stream stream, string format, params object[] args)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            await stream.WriteStrAsync(string.Format(format, args));
        }

        public static async Task WriteLineAsync(this Stream stream, string str, Encoding encoding = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            encoding = encoding ?? Encoding.UTF8;

            await stream.WriteBytesAsync(encoding.GetBytes($"{str}\r\n"));
        }

        public static async Task WriteStrAsync(this Stream stream, string str, Encoding encoding = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            encoding = encoding ?? Encoding.UTF8;

            await stream.WriteBytesAsync(encoding.GetBytes(str));
        }

        public static async Task WriteBytesAsync(this Stream stream, byte[] data)
        {
            await stream.WriteAsync(data, 0, data.Length);
        }

        public static async Task<object> ReadJsonBufferedAsync(this NetworkStream stream, Type type, int payloadSize, Encoding encoding = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (type == null) throw new ArgumentNullException(nameof(type));

            encoding = encoding ?? Encoding.UTF8;

            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyBufferedToAsync(memoryStream, payloadSize);

                return PapercutIPCommSerializer.FromJson(type, encoding.GetString(memoryStream.ToArray()));
            }
        }

        public static async Task<int> CopyBufferedToAsync(
            this NetworkStream source,
            Stream destination,
            int? maxBytes = null,
            int bufferLength = 0xFFF)
        {
            var buffer = new byte[bufferLength];
            int totalBytes = 0;

            do
            {
                int bytesRead = await source.ReadAsync(buffer, 0, bufferLength);

                if (bytesRead == 0) break;

                await destination.WriteAsync(buffer, 0, bytesRead);

                totalBytes += bytesRead;

                if (maxBytes.HasValue && totalBytes >= maxBytes.Value)
                {
                    break;
                }
            }
            while (source.DataAvailable);

            return totalBytes;
        }
    }
}