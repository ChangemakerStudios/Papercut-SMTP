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
    using System.Text;

    public static class StreamExtensions
    {
        public static string ReadString(
            this Stream stream,
            int bufferSize = 0xFF0,
            Encoding encoding = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var serverbuff = new byte[bufferSize];

            encoding = encoding ?? Encoding.UTF8;

            int count = stream.Read(serverbuff, 0, bufferSize);

            return count == 0 ? string.Empty : encoding.GetString(serverbuff, 0, count);
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

            stream.WriteBytes(encoding.GetBytes(str + "\r\n"));
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

        public static Stream CopyBufferedTo(
            this Stream source,
            Stream destination,
            int bufferLength = 0xFFF)
        {
            var buffer = new byte[bufferLength];
            int bytesRead = source.Read(buffer, 0, bufferLength);

            // write the required bytes
            while (bytesRead > 0)
            {
                destination.Write(buffer, 0, bytesRead);
                bytesRead = source.Read(buffer, 0, bufferLength);
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