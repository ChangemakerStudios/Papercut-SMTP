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


using Microsoft.Extensions.Configuration;

namespace Papercut.Service.Infrastructure.Configuration;

/// <summary>
/// Optional "DataDirectory" setting: where the service keeps its settings file and rules.json.
/// The desktop shell sets it to the per-user data folder, because Velopack replaces the exe
/// folder on every update. Unset (service zip, Docker) keeps both beside the exe.
/// </summary>
public static class ServiceDataDirectory
{
    public const string ConfigKey = "DataDirectory";

    public static string? Get(IConfiguration configuration)
    {
        var value = configuration[ConfigKey];

        return string.IsNullOrWhiteSpace(value) ? null : Path.GetFullPath(Environment.ExpandEnvironmentVariables(value));
    }

    public static string? GetFilePath(IConfiguration configuration, string fileName)
    {
        var directory = Get(configuration);

        if (directory == null) return null;

        Directory.CreateDirectory(directory);

        return Path.Combine(directory, fileName);
    }
}
