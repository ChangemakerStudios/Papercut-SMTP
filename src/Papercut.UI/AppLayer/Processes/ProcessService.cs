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


namespace Papercut.AppLayer.Processes;

public class ProcessService(ILogger logger)
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

    public ExecutionResult OpenFile(string filePath)
    {
        try
        {
            // Set WorkingDirectory to file's directory to avoid path resolution issues on Windows 11
            // Explicitly set Verb to "open" for reliability with shell file associations
            var directory = Path.GetDirectoryName(filePath) ?? Path.GetTempPath();
            var processStartInfo = new ProcessStartInfo(filePath)
            {
                UseShellExecute = true,
                WorkingDirectory = directory,
                Verb = "open"
            };

            Process.Start(processStartInfo);

            return ExecutionResult.Success();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failure Opening File: {FilePath}", filePath);

            return ExecutionResult.Failure($"Failed to open file '{filePath}': {ex.Message}");
        }
    }

    public ExecutionResult OpenUri(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });

            return ExecutionResult.Success();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failure Opening URI: {Uri}", uri.AbsoluteUri);

            return ExecutionResult.Failure($"Failed to open URI '{uri.AbsoluteUri}': {ex.Message}");
        }
    }

    #region Begin Static Container Registrations

    private static void Register(ContainerBuilder builder)
    {
        builder.RegisterType<ProcessService>().AsSelf();
    }

    #endregion
}