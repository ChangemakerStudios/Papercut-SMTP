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

namespace Papercut.Network.Helpers
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Text;

    using Papercut.Common.Extensions;
    using Papercut.Core.Infrastructure.Json;

    public static class PapercutProtocolHelpers
    {
        public static object ReadObj(this Socket socket, Type type, int payloadSize)
        {
            if (socket == null) throw new ArgumentNullException(nameof(socket));
            if (type == null) throw new ArgumentNullException(nameof(type));

            using (var networkStream = new NetworkStream(socket, false))
            using (var memoryStream = new MemoryStream())
            {
                networkStream.CopyBufferedLimited(memoryStream, payloadSize);
                string incoming = new ASCIIEncoding().GetString(memoryStream.ToArray());

                return incoming.FromJson(type);
            }
        }
    }
}