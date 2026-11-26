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

using Microsoft.Extensions.Configuration;

namespace Papercut.Service.Application.Rules;

/// <summary>
///     Invoke Process rules start a program on the service's machine. They
///     are off unless the operator turns them on.
/// </summary>
public static class InvokeProcessRuleSettings
{
    public const string EnabledSettingKey = "EnableInvokeProcessRule";

    /// <summary>
    ///     Read from configuration only (appsettings.json, environment
    ///     variables, command line), never from the setting store: the
    ///     settings API writes the setting store without authentication, so
    ///     a remote caller could otherwise switch this on.
    /// </summary>
    public static bool IsEnabled(IConfiguration configuration)
    {
        return bool.TryParse(configuration[EnabledSettingKey], out var enabled) && enabled;
    }
}
