// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2024 Jaben Cargman
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


//Copyright (c) 2014 Robert Blackbourn

//Permission is hereby granted, free of charge, to any person obtaining a copy
//    of this software and associated documentation files (the "Software"), to deal
//    in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//    furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//    copies or substantial portions of the Software.

//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.


using System.Net.Sockets;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Papercut.Infrastructure.IPComm.Helpers
{
    /// <summary>
    /// Used from the JetBlack.Network Project:
    /// https://github.com/rob-blackbourn/JetBlack.Network
    /// MIT licensed
    /// THANK YOU -- no nuget so I had to copy and paste
    /// </summary>
    public static class ClientExtensions
    {
        public static ISubject<ArraySegment<byte>, ArraySegment<byte>> ToClientSubject(
            [NotNull] this Socket socket, int size, SocketFlags socketFlags = SocketFlags.None)
        {
            if (socket == null) throw new ArgumentNullException(nameof(socket));

            return Subject.Create<ArraySegment<byte>, ArraySegment<byte>>(
                socket.ToClientObserver(socketFlags),
                socket.ToClientObservable(size, socketFlags));
        }

        public static IObservable<ArraySegment<byte>> ToClientObservable([NotNull] this Socket socket, int size, SocketFlags socketFlags = SocketFlags.None)
        {
            if (socket == null) throw new ArgumentNullException(nameof(socket));

            return Observable.Create<ArraySegment<byte>>(async (observer, token) =>
            {
                var buffer = new byte[size];
                var arraySegment = new ArraySegment<byte>(buffer);

                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        var received = await socket.ReceiveAsync(arraySegment, socketFlags);
                        if (received == 0)
                            break;

                        observer.OnNext(new ArraySegment<byte>(buffer, 0, received));
                    }

                    observer.OnCompleted();
                }
                catch (Exception error)
                {
                    observer.OnError(error);
                }
            });
        }

        public static IObserver<ArraySegment<byte>> ToClientObserver(this Socket socket, SocketFlags socketFlags = SocketFlags.None)
        {
            return Observer.Create<ArraySegment<byte>>(async buffer =>
            {
                var bufferArray = buffer.Array;

                if (bufferArray == null)
                {
                    return;
                }

                var sent = 0;
                while (sent < buffer.Count)
                {
                    sent += await socket.SendAsync(
                                new ArraySegment<byte>(bufferArray, sent, buffer.Count - sent),
                                socketFlags);
                }
            });
        }
    }
}