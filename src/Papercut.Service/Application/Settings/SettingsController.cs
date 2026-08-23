// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
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
using System.Net.NetworkInformation;
using System.Net.Sockets;

using Microsoft.Extensions.Configuration;

using Papercut.Core.Domain.Network.Smtp;
using Papercut.Service.Application.Mcp;
using Papercut.Service.Domain;

namespace Papercut.Service.Application.Settings;

/// <summary>
///     Server-side settings, mirroring the desktop Options window's SMTP
///     section. SMTP IP/Port changes apply live (the listener rebinds);
///     the MCP toggle takes effect on restart.
/// </summary>
[Route("api/[controller]")]
public class SettingsController(
    ISmtpServerOptionsProvider smtpOptionsProvider,
    ISettingStore settingStore,
    IConfiguration configuration,
    IMessageBus messageBus) : ControllerBase
{
    [HttpGet]
    public SettingsDto Get()
    {
        var smtpSettings = smtpOptionsProvider.Settings;

        return new SettingsDto
        {
            SmtpIP = smtpSettings.IP,
            SmtpPort = smtpSettings.Port,
            McpEnabled = McpServerSettings.IsEnabled(settingStore, configuration),
            AvailableIPs = GetAvailableIPs()
        };
    }

    [HttpPut]
    public async Task<ActionResult<UpdateSettingsResponse>> Update([FromBody] UpdateSettingsRequest request, CancellationToken token = default)
    {
        var response = new UpdateSettingsResponse();

        if (request.SmtpPort is < 1 or > 65535)
        {
            return BadRequest(new { error = "SMTP port must be between 1 and 65535" });
        }

        var currentSmtp = smtpOptionsProvider.Settings;
        var newIp = request.SmtpIP?.Trim();
        var newPort = request.SmtpPort;

        if (newIp is not null && newIp != "Any" && !IPAddress.TryParse(newIp, out _))
        {
            return BadRequest(new { error = $"'{newIp}' is not a valid IP address" });
        }

        var smtpChanged = (newIp is not null && newIp != currentSmtp.IP)
                          || (newPort is not null && newPort != currentSmtp.Port);

        if (smtpChanged)
        {
            // SmtpServerManager persists the values and rebinds the listener
            await messageBus.PublishAsync(
                new SmtpServerBindEvent(newIp ?? currentSmtp.IP, newPort ?? currentSmtp.Port),
                token);

            response.SmtpRebound = true;
        }

        if (request.McpEnabled is { } mcpEnabled
            && mcpEnabled != McpServerSettings.IsEnabled(settingStore, configuration))
        {
            settingStore.Set(McpServerSettings.EnabledSettingKey, mcpEnabled.ToString());
            settingStore.Save();

            // The MCP endpoint is mapped at startup, so this needs a restart
            response.McpRequiresRestart = true;
        }

        return response;
    }

    static List<string> GetAvailableIPs()
    {
        var ips = new List<string> { "Any" };

        try
        {
            ips.AddRange(
                NetworkInterface.GetAllNetworkInterfaces()
                    .Where(i => i.OperationalStatus == OperationalStatus.Up)
                    .SelectMany(i => i.GetIPProperties().UnicastAddresses)
                    .Select(a => a.Address)
                    .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
                    .Select(a => a.ToString())
                    .Distinct());
        }
        catch (Exception)
        {
            // Fall back to the loopback address if interface enumeration fails
            ips.Add("127.0.0.1");
        }

        return ips;
    }

    [PublicAPI]
    public class SettingsDto
    {
        public string SmtpIP { get; set; } = "Any";

        public int SmtpPort { get; set; }

        public bool McpEnabled { get; set; }

        public List<string> AvailableIPs { get; set; } = [];
    }

    [PublicAPI]
    public class UpdateSettingsRequest
    {
        public string? SmtpIP { get; set; }

        public int? SmtpPort { get; set; }

        public bool? McpEnabled { get; set; }
    }

    [PublicAPI]
    public class UpdateSettingsResponse
    {
        public bool SmtpRebound { get; set; }

        public bool McpRequiresRestart { get; set; }
    }
}
