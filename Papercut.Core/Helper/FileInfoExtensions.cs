// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2014 Jaben Cargman
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

namespace Papercut.Core.Helper
{
    using System;
    using System.IO;

    public static class FileInfoExtensions
    {
        public static bool CanOpenFile(this FileInfo file)
        {
            if (file == null) throw new ArgumentNullException("file");

            FileStream fileStream = null;

            try
            {
                fileStream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                fileStream = null;
            }
            catch (IOException)
            {
                return false;
            }
            finally
            {
                if (fileStream != null) fileStream.Dispose();
            }

            return true;
        }

        public static bool TryReadFile(this FileInfo file, out byte[] fileBytes)
        {
            if (file == null) throw new ArgumentNullException("file");

            fileBytes = null;
            FileStream fileStream = null;

            try
            {
                fileStream = file.OpenRead();
                using (var ms = new MemoryStream())
                {
                    fileStream.CopyTo(ms);
                    fileBytes = ms.ToArray();
                    fileStream = null;
                }
            }
            catch (IOException)
            {
                // the file is unavailable because it is still being written by another thread or process
                return false;
            }
            finally
            {
                if (fileStream != null)
                    fileStream.Dispose();
            }

            return true;
        }
    }
}