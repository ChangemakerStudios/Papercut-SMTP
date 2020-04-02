// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2020 Jaben Cargman
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

namespace Papercut.App.WebApi.Helpers
{
    using System.IO;
    using System.Linq;

    class FileHelper
    {
        public static string NormalizeFilename(string filename)
        {
            var validFilename = RemoveInvalidFileNameChars(filename);
            return validFilename.Replace(" ", "_");
        }

        static string RemoveInvalidFileNameChars(string filename)
        {
            return Path.GetInvalidFileNameChars().Aggregate(filename,
                (current, invalidChar) => current.Replace(invalidChar.ToString(), string.Empty));
        }
    }
}