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

namespace Papercut.Common.Extensions
{
    using System;
    using System.IO;

    public static class FileInfoExtensions
    {
        public static bool CanReadFile(this FileInfo file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            try
            {
                using (var fileStream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    fileStream.Close();
                }
            }
            catch (IOException)
            {
                return false;
            }

            return true;
        }

        public static bool TryGetReadFileStream(this FileInfo file, out Stream fileStream)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            fileStream = Stream.Null;

            try
            {
                fileStream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (IOException)
            {
                // the file is unavailable because it is still being written by another thread or process
                return false;
            }

            return true;
        }
    }
}