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


using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace Papercut.Service.Infrastructure.Logging;

/// <summary>
///     Keeps a bounded in-memory ring buffer of recent log events with
///     monotonic sequence numbers so the web UI's Log view can tail the
///     service log over HTTP (GET api/logs?after=seq). Same hook the
///     desktop uses for its log window (see UiLogSinkQueue).
/// </summary>
public class WebLogBufferSink : ILoggerSettings, ILogEventSink
{
    const int MaxEntries = 1000;

    static readonly object _sync = new();

    static readonly Queue<BufferedLogEntry> _buffer = new();

    static long _sequence;

    public void Configure(LoggerConfiguration loggerConfiguration)
    {
        var minimumLevel = LogEventLevel.Information;
#if DEBUG
        minimumLevel = LogEventLevel.Debug;
#endif
        loggerConfiguration.WriteTo.Sink(this, minimumLevel);
    }

    public void Emit(LogEvent logEvent)
    {
        Append(logEvent);
    }

    static void Append(LogEvent logEvent)
    {
        var entry = new BufferedLogEntry
        {
            Timestamp = logEvent.Timestamp,
            Level = logEvent.Level.ToString(),
            Message = logEvent.RenderMessage(),
            Exception = logEvent.Exception?.ToString()
        };

        lock (_sync)
        {
            entry.Seq = ++_sequence;
            _buffer.Enqueue(entry);

            while (_buffer.Count > MaxEntries)
            {
                _buffer.Dequeue();
            }
        }
    }

    public static (IReadOnlyList<BufferedLogEntry> Entries, long LastSeq) GetEntriesAfter(long afterSeq, int take)
    {
        lock (_sync)
        {
            var entries = _buffer.Where(e => e.Seq > afterSeq).Take(Math.Clamp(take, 1, MaxEntries)).ToList();
            return (entries, _sequence);
        }
    }

    [PublicAPI]
    public class BufferedLogEntry
    {
        public long Seq { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string Level { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string? Exception { get; set; }
    }

    #region Begin Static Container Registrations

    /// <summary>
    /// Called dynamically from the RegisterStaticMethods() call in the container module.
    /// </summary>
    /// <param name="builder"></param>
    [UsedImplicitly]
    static void Register(ContainerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RegisterType<WebLogBufferSink>().As<ILoggerSettings>().AsSelf();
    }

    #endregion
}
