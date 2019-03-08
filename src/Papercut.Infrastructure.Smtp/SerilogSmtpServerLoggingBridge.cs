namespace Papercut.Infrastructure.Smtp
{
    using System;

    using Serilog.Events;

    using global::SmtpServer;

    public class SerilogSmtpServerLoggingBridge : global::SmtpServer.ILogger
    {
        private readonly global::Serilog.ILogger _serilog;

        public SerilogSmtpServerLoggingBridge(global::Serilog.ILogger serilog)
        {
            this._serilog = serilog.ForContext<global::SmtpServer.SmtpServer>();
        }

        public void LogVerbose(string format, params object[] args)
        {
            this._serilog.Write(LogEventLevel.Verbose, (Exception)null, format, args);
        }
    }
}