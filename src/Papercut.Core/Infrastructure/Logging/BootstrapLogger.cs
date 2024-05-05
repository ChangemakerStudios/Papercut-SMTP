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


using Papercut.Core.Infrastructure.CommandLine;

using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Papercut.Core.Infrastructure.Logging;

public static class BootstrapLogger
{
    static BootstrapLogger()
    {
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainOnUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
    }

    static void TaskSchedulerOnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        if (!args.Observed) Log.Warning(args.Exception, "Unobserved Task Exception");

        args.SetObserved();
    }

    public static ILogger CreateBootstrapLogger(string[] args)
    {
        string logFilePath = Path.Combine(AppConstants.AppDataDirectory,
            "Logs",
            "PapercutSmtpFailure.json");

        var logger =
            new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Verbose()
#else
                .MinimumLevel.Information()
#endif
                .Enrich.With<EnvironmentEnricher>()
                .WriteTo.Console()
                .WriteTo.File(new CompactJsonFormatter(), logFilePath, LogEventLevel.Information)
                .ReadFrom.KeyValuePairs(ArgumentParser.GetArgsKeyValue(args))
                .CreateLogger();

        logger.Debug("JSON Bootstrap Log File: {LogFilePathName}", logFilePath);

        Log.Logger = logger;

        return logger;
    }

    static void OnCurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        if (args.IsTerminating) Log.Fatal(args.ExceptionObject as Exception, "Unhandled Exception");
        else Log.Information(args.ExceptionObject as Exception, "Non-Fatal Unhandled Exception");
    }
}