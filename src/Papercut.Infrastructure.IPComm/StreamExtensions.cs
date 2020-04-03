// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2020 Jaben Cargman
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

    using Network;

    public static class StreamExtensions
    { 
        public static byte[] ReadBuffered(
            this NetworkStream stream,
            int? maxBytes = null,
            int bufferSize = 0xFFF)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            using (var ms = new MemoryStream())
            {
                stream.CopyBufferedTo(ms, maxBytes, bufferSize);
                return ms.ToArray();
            }
        }

        public static string ReadStringBuffered(
            this NetworkStream stream,
            int? maxBytes = null,
            int bufferSize = 0xFFF,
            Encoding encoding = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            encoding = encoding ?? Encoding.UTF8;

            return encoding.GetString(stream.ReadBuffered(maxBytes, bufferSize));
        }

        public static object ReadJsonBuffered(
            this NetworkStream stream,
            Type type,
            int? maxBytes = null,
            int bufferSize = 0xFFF, Encoding encoding = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            return PapercutIPCommSerializer.FromJson(type, stream.ReadStringBuffered(maxBytes, bufferSize, encoding));
        }

        public static void WriteFormat(this Stream stream, string format, params object[] args)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            stream.WriteStr(string.Format(format, args));
        }

        public static void WriteLine(this Stream stream, string str, Encoding encoding = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            encoding = encoding ?? Encoding.UTF8;

            stream.WriteBytes(encoding.GetBytes($"{str}\r\n"));
        }

        public static void WriteStr(this Stream stream, string str, Encoding encoding = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            encoding = encoding ?? Encoding.UTF8;

            stream.WriteBytes(encoding.GetBytes(str));
        }

        public static void WriteBytes(this Stream stream, byte[] data)
        {
            stream.Write(data, 0, data.Length);
        }

        public static object ReadJsonBuffered(this NetworkStream stream, Type type, int payloadSize, Encoding encoding = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (type == null) throw new ArgumentNullException(nameof(type));

            encoding = encoding ?? Encoding.UTF8;

            using (var memoryStream = new MemoryStream())
            {
                stream.CopyBufferedTo(memoryStream, payloadSize);

                return PapercutIPCommSerializer.FromJson(type, encoding.GetString(memoryStream.ToArray()));
            }
        }

        public static int CopyBufferedTo(
            this NetworkStream source,
            Stream destination,
            int? maxBytes = null,
            int bufferLength = 0xFFF)
        {
            var buffer = new byte[bufferLength];
            int totalBytes = 0;

            do
            {
                int bytesRead = source.Read(buffer, 0, bufferLength);

                if (bytesRead == 0) break;

                destination.Write(buffer, 0, bytesRead);

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