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


namespace Papercut.Core.Infrastructure.CommandLine;

public static class ArgumentParser
{
    public static IEnumerable<KeyValuePair<string, string>> GetArgsKeyValue(string[] args)
    {
        var startingValues = new[] { "--", "-", "/" };

        var cleanedValues = new List<string>();

        foreach (var a in args)
        {
            var cleaned = a;

            var starting = startingValues.FirstOrDefault(s => a.StartsWith(s));
            if (!string.IsNullOrEmpty(starting))
            {
                // remove it
                cleaned = a.Substring(
                    starting.Length,
                    a.Length - starting.Length).Trim();

                if (cleanedValues.Count % 2 == 1)
                {
                    // odd, add an empty value/pair
                    cleanedValues.Add(null);
                }

            }

            if (cleaned.Contains("="))
            {
                // needs to be split
                var pair = cleaned.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim()).ToArray();

                if (pair.Length == 2)
                {
                    // all good, return it
                    yield return KeyValuePair.Create(pair[0], pair[1]);
                }
            }
            else
            {
                cleanedValues.Add(cleaned);
            }
        }

        var count = cleanedValues.Count % 2 == 1
            ? cleanedValues.Count - 1
            : cleanedValues.Count;

        for (int i = 0; i < count; i += 2)
        {
            if (cleanedValues[i] == null || cleanedValues[i + 1] == null) continue;

            yield return KeyValuePair.Create(cleanedValues[i], cleanedValues[i + 1]);
        }
    }
}