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

using System.Diagnostics;

using Autofac;

using Papercut.Core.Domain.Message;
using Papercut.Core.Domain.Rules;
using Papercut.Core.Infrastructure.Logging;
using Papercut.Rules.Domain.Invoking;

using Serilog.Context;

namespace Papercut.Rules.App.Invoking;

public class InvokeProcessRuleDispatch(ILogger logger) : IRuleDispatcher<InvokeProcessRule>
{
    public async Task DispatchAsync(InvokeProcessRule rule, MessageEntry messageEntry, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(rule.ProcessToRun))
        {
            logger.Warning("Invoke Process Rule 'Process to Run' is not set -- nothing done");

            return;
        }

        try
        {
            var arguments =
                (rule.ProcessCommandLine ?? string.Empty).Replace("%e", messageEntry.File);

            using var _01 = LogContext.PushProperty("Arguments", arguments);
            using var _02 = LogContext.PushProperty("ProcessToRun", rule.ProcessToRun);

            using var process = new Process();

            process.StartInfo.FileName = rule.ProcessToRun;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            if (!process.Start())
            {
                logger.Error("Process {ProcessToRun} Failed to Start", rule.ProcessToRun);

                return;
            }

            await process.WaitForExitAsync(token);

            string output = await process.StandardOutput.ReadToEndAsync(token);
            string error = await process.StandardError.ReadToEndAsync(token);

            if (process.ExitCode != 0)
            {
                logger.Warning(
                    "Process {ProcessToRun} Process failed with exit code {ExitCode}: {Error}",
                    rule.ProcessToRun, process.ExitCode, error);
            }

            if (!string.IsNullOrWhiteSpace(output))
            {
                logger.Information(
                    "Process {ProcessToRun} Ran Successfully. Output {ProcessOutput}",
                    rule.ProcessToRun, output);
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception ex) when (logger.ErrorWithContext(ex, "Invoke Process Rule Failed to Start Process"))
        {
        }
    }

    #region Begin Static Container Registrations

    /// <summary>
    /// Called dynamically from the RegisterStaticMethods() call in the container module.
    /// </summary>
    /// <param name="builder"></param>
    [UsedImplicitly]
    static void Register(ContainerBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.RegisterType<InvokeProcessRuleDispatch>()
            .As<IRuleDispatcher<InvokeProcessRule>>().AsSelf().InstancePerDependency();
    }

    #endregion
}