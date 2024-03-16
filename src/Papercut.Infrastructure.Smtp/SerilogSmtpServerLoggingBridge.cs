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


using Serilog.Events;

using ILogger = SmtpServer.ILogger;

namespace Papercut.Infrastructure.Smtp
{
    public class SerilogSmtpServerLoggingBridge : ILogger
    {
        private readonly global::Serilog.ILogger _serilog;

        public SerilogSmtpServerLoggingBridge(global::Serilog.ILogger serilog)
        {
            this._serilog = serilog.ForContext<SmtpServer.SmtpServer>();
        }

        public void LogVerbose(string format, params object[] args)
        {
            this._serilog.Write(LogEventLevel.Verbose, (Exception)null, format, args);
        }
    }
}