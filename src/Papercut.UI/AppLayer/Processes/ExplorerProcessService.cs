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


using System.Diagnostics;

using Autofac;

namespace Papercut.AppLayer.Processes;

public class ExplorerProcessService
{
    public void OpenFolder(string folder)
    {
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", folder));
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.Message.Contains("Access is denied"))
        {
            // access denied -- run elevated
            Process.Start(new ProcessStartInfo("explorer.exe", folder)
                          {
                              Verb = "runas",
                              UseShellExecute = true
                          });
        }
    }

    #region Begin Static Container Registrations

    static void Register(ContainerBuilder builder)
    {
        builder.RegisterType<ExplorerProcessService>().AsSelf();
    }

    #endregion
}