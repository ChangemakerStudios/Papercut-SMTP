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
using System.Windows;

using Autofac;

namespace Papercut.AppLayer.Processes;

public class ProcessService
{
    private readonly ILogger _logger;

    public ProcessService(ILogger logger)
    {
        this._logger = logger;
    }

    public void Start(string pathOrFileName)
    {
        try
        {
            Process.Start(pathOrFileName);
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.Message.Contains("Access is denied"))
        {
            this._logger.Warning(ex, "Access denied to folder: {Folder}", pathOrFileName);

            MessageBox.Show($"Failed to open path '{pathOrFileName}' due to permissions", "Access denied to path");
        }
    }

    #region Begin Static Container Registrations

    static void Register(ContainerBuilder builder)
    {
        builder.RegisterType<ProcessService>().AsSelf();
    }

    #endregion
}