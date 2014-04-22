/*  
 * Papercut
 *
 *  Copyright © 2008 - 2012 Ken Robertson
 *  Copyright © 2013 - 2014 Jaben Cargman
 *  
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *  
 *  http://www.apache.org/licenses/LICENSE-2.0
 *  
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *  
 */

namespace Papercut.Core
{
    using System;
    using System.Reflection;

    using Serilog;
    using Serilog.Events;

    /// <summary>
    ///     Summary description for Logger.
    /// </summary>
    public static class Logger
    {
        static readonly Lazy<ILogger> _log;

        static Logger()
        {
            _log = new Lazy<ILogger>(() => Serilog.Log.ForContext(MethodBase.GetCurrentMethod().DeclaringType));
        }

        /// <summary>
        ///     The log.
        /// </summary>
        static ILogger Log
        {
            get
            {
                return _log.Value;
            }
        }

        /// <summary>
        ///     The write.
        /// </summary>
        /// <param name="message">
        ///     The message.
        /// </param>
        public static void Write(string message)
        {
            if (Log.IsEnabled(LogEventLevel.Information))
            {
                Log.Information("{Message}", message);
            }
        }

        /// <summary>
        ///     The write.
        /// </summary>
        /// <param name="message">
        ///     The message.
        /// </param>
        /// <param name="connectionId">
        ///     The connection id.
        /// </param>
        public static void Write(string message, int connectionId)
        {
            if (Log.IsEnabled(LogEventLevel.Information))
            {
                Log.Information("[{ConnectionId}] - {Message}", connectionId, message);
            }
        }

        /// <summary>
        ///     The write debug.
        /// </summary>
        /// <param name="message">
        ///     The message.
        /// </param>
        public static void WriteDebug(string message)
        {
            if (Log.IsEnabled(LogEventLevel.Debug))
            {
                Log.Debug("{Message}", message);
            }
        }

        /// <summary>
        ///     The write debug.
        /// </summary>
        /// <param name="message">
        ///     The message.
        /// </param>
        /// <param name="connectionId">
        ///     The connection id.
        /// </param>
        public static void WriteDebug(string message, int connectionId)
        {
            if (Log.IsEnabled(LogEventLevel.Debug))
            {
                Log.Debug("[{ConnectionId}] - {Message}", connectionId, message);
            }
        }

        /// <summary>
        ///     The write error.
        /// </summary>
        /// <param name="message">
        ///     The message.
        /// </param>
        /// <param name="e">
        ///     The e.
        /// </param>
        public static void WriteError(string message, Exception e)
        {
            if (Log.IsEnabled(LogEventLevel.Error))
            {
                Log.Error(e, "{Message}", message);
            }
        }

        /// <summary>
        ///     The write error.
        /// </summary>
        /// <param name="message">
        ///     The message.
        /// </param>
        /// <param name="e">
        ///     The e.
        /// </param>
        /// <param name="connectionId">
        ///     The connection id.
        /// </param>
        public static void WriteError(string message, Exception e, int connectionId)
        {
            if (Log.IsEnabled(LogEventLevel.Error))
            {
                Log.Error(e, "[{ConnectionId}] - {Message}", connectionId, message);
            }
        }

        /// <summary>
        ///     The write warning.
        /// </summary>
        /// <param name="message">
        ///     The message.
        /// </param>
        public static void WriteWarning(string message)
        {
            if (Log.IsEnabled(LogEventLevel.Warning))
            {
                Log.Warning("{Message}", message);
            }
        }

        /// <summary>
        ///     The write warning.
        /// </summary>
        /// <param name="message">
        ///     The message.
        /// </param>
        /// <param name="connectionId">
        ///     The connection id.
        /// </param>
        public static void WriteWarning(string message, int connectionId)
        {
            if (Log.IsEnabled(LogEventLevel.Warning))
            {
                Log.Error("[{ConnectionId}] - {Message}", connectionId, message);
            }
        }
    }
}