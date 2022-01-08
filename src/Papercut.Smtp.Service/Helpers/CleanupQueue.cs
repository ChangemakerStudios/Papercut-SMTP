// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2022 Jaben Cargman
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


using Autofac.Util;

namespace Papercut.Smtp.Service.Helpers;

public class CleanupQueue : Disposable
{
    readonly ConcurrentQueue<string> _fileNames = new();

    public void EnqueueFile(string filename)
    {
        this._fileNames.Enqueue(filename);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            while (this._fileNames.TryDequeue(out string fileName))
            {
                try
                {
                    File.Delete(fileName);
                }
                catch
                {
                    // ignored
                }
            }
        }
    }

    #region Begin Static Container Registrations

    /// <summary>
    /// Called dynamically from the RegisterStaticMethods() call in the container module.
    /// </summary>
    /// <param name="builder"></param>
    [UsedImplicitly]
    static void Register([NotNull] ContainerBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.RegisterType<CleanupQueue>().AsSelf().SingleInstance();
    }

    #endregion
}