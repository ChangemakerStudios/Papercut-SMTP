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


using Papercut.Common.Domain;

namespace Papercut.Common.Extensions
{
    public static class FileInfoExtensions
    {
        public static async Task<bool> CanReadFile(this FileInfo file)
        {
            ArgumentNullException.ThrowIfNull(file, nameof(file));

            try
            {
                await using (var _ = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read)) { }
            }
            catch (IOException)
            {
                return false;
            }

            return true;
        }

        public static async Task<ExecutionResult<byte[]>> TryReadFile(this FileInfo file)
        {
            ArgumentNullException.ThrowIfNull(file, nameof(file));

            try
            {
                using var ms = new MemoryStream();
                await using var fileStream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);

                await fileStream.CopyToAsync(ms);

                return ExecutionResult.Success(ms.ToArray());
            }
            catch (IOException)
            {
                // the file is unavailable because it is still being written by another thread or process
                return ExecutionResult.Failure<byte[]>("File is unavailable");
            }
        }
    }
}