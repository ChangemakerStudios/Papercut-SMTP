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


using Microsoft.AspNetCore.Mvc.Filters;

using ModelContextProtocol;

namespace Papercut.Service.Application.Controllers;

/// <summary>
///     Derives from <see cref="McpException" /> so the same throw surfaces as a tool error
///     over MCP and, via <see cref="MessageNotFoundExceptionFilterAttribute" />, a 404 over REST.
/// </summary>
public class MessageNotFoundException(string messageId) : McpException($"Message '{messageId}' was not found");

public class MessageNotFoundExceptionFilterAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is MessageNotFoundException)
        {
            context.Result = new NotFoundResult();
            context.ExceptionHandled = true;
        }
    }
}
