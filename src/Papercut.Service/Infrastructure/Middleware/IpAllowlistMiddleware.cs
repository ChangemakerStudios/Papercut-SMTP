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


using Microsoft.AspNetCore.Http;
using Papercut.Infrastructure.Smtp;
using Serilog;

namespace Papercut.Service.Infrastructure.Middleware;

public class IpAllowlistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IpAllowlistValidator _ipValidator;
    private readonly ILogger _logger;

    public IpAllowlistMiddleware(
        RequestDelegate next,
        string allowedHosts,
        ILogger logger)
    {
        _next = next;
        _ipValidator = new IpAllowlistValidator(allowedHosts);
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress;

        if (remoteIp == null)
        {
            _logger.Warning("Unable to determine remote IP address for HTTP request");
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Access denied: Unable to determine client IP");
            return;
        }

        if (!_ipValidator.IsAllowed(remoteIp))
        {
            _logger.Warning(
                "Rejected HTTP request from {RemoteIp} - IP not in allowlist. Path: {Path}",
                remoteIp,
                context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Access denied: Your IP address is not allowed");
            return;
        }

        await _next(context);
    }
}
