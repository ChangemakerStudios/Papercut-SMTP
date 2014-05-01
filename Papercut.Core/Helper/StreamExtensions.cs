/*  
 * Papercut
 *
 *  Copyright © 2008 - 2012 Ken Robertson
 *  Copyright © 2013 - 2014 Jaben Cargman
 *  
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *  
 *  http://www.apache.org/licenses/LICENSE-2.0
 *  
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *  
 */

namespace Papercut.Core.Helper
{
    using System.IO;

    public static class StreamExtensions
    {
        public static Stream CopyBufferedTo(this Stream source, Stream destination, int bufferLength = 0xFFF)
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
    }
}