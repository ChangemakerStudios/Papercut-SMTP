// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2026 Jaben Cargman
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


using System.Net;
using System.Net.Sockets;

using AwesomeAssertions;

using NUnit.Framework;

using Papercut.Core.Domain.Application;
using Papercut.Core.Domain.Network;

using ServiceProvider = SmtpServer.ComponentModel.ServiceProvider;

namespace Papercut.Infrastructure.Smtp.Tests;

[TestFixture]
public class PapercutSmtpServerTests
{
    private static PapercutSmtpServer CreateServer() =>
        new(new TestAppMeta(), Serilog.Core.Logger.None, options => new SmtpServer.SmtpServer(options, new ServiceProvider()));

    private static TcpListener OccupyFreePort(out int port)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        port = ((IPEndPoint)listener.LocalEndpoint).Port;
        return listener;
    }

    private static async Task<bool> WaitForGreetingAsync(int port, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(IPAddress.Loopback, port);
                return true;
            }
            catch (SocketException)
            {
                await Task.Delay(100);
            }
        }

        return false;
    }

    [Test]
    public async Task StartAsync_WhenPortIsInUse_BindsOnceItIsFree()
    {
        var holder = OccupyFreePort(out var port);
        await using var server = CreateServer();

        await server.StartAsync(new EndpointDefinition("127.0.0.1", port));
        await Task.Delay(300);

        holder.Stop();

        (await WaitForGreetingAsync(port, TimeSpan.FromSeconds(10))).Should().BeTrue();
    }

    [Test]
    public async Task StopAsync_WhenPortIsInUse_DoesNotHang()
    {
        using var holder = OccupyFreePort(out var port);
        var server = CreateServer();

        await server.StartAsync(new EndpointDefinition("127.0.0.1", port));
        await Task.Delay(300);

        var stop = server.StopAsync();

        (await Task.WhenAny(stop, Task.Delay(TimeSpan.FromSeconds(5)))).Should().BeSameAs(stop);
        server.IsActive.Should().BeFalse();
    }

    private sealed class TestAppMeta : IAppMeta
    {
        public string AppName => "Papercut.Tests";

        public string AppVersion => "1.0.0";
    }
}
