namespace Papercut.Service.Infrastructure.SmtpServer
{
    using System;

    using global::SmtpServer;

    using Serilog.Events;

    public class SerilogSmtpServerLoggingBridge : global::SmtpServer.ILogger
    {
        private readonly global::Serilog.ILogger _serilog;

        public SerilogSmtpServerLoggingBridge(global::Serilog.ILogger serilog)
        {
            this._serilog = serilog.ForContext<SmtpServer>();
        }

        public void LogVerbose(string format, params object[] args)
        {
            this._serilog.Write(LogEventLevel.Verbose, (Exception)null, format, args);
        }
    }
}