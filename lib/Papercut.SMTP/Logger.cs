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

#region Using

using log4net.Config;

#endregion

[assembly: XmlConfigurator(Watch = true)]

namespace Papercut.SMTP
{
	#region Using

	using System;
	using System.Diagnostics;
	using System.Reflection;

	using log4net;

	#endregion

	/// <summary>
	/// Summary description for Logger.
	/// </summary>
	public static class Logger
	{
		#region Constants and Fields

		/// <summary>
		/// The log.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		#endregion

		#region Public Methods and Operators

		/// <summary>
		/// The write.
		/// </summary>
		/// <param name="message">
		/// The message.
		/// </param>
		public static void Write(string message)
		{
			if (log.IsInfoEnabled)
			{
				log.Info(string.Format("- {0}", message));
			}
		}

		/// <summary>
		/// The write.
		/// </summary>
		/// <param name="message">
		/// The message.
		/// </param>
		/// <param name="connectionId">
		/// The connection id.
		/// </param>
		public static void Write(string message, int connectionId)
		{
			if (log.IsInfoEnabled)
			{
				log.Info(string.Format("[{0}] - {1}", connectionId, message));
			}
		}

		/// <summary>
		/// The write debug.
		/// </summary>
		/// <param name="message">
		/// The message.
		/// </param>
		public static void WriteDebug(string message)
		{
			if (log.IsDebugEnabled)
			{
				log.Debug(string.Format("- {0}", message));
			}

			Debug.WriteLine(message);
		}

		/// <summary>
		/// The write debug.
		/// </summary>
		/// <param name="message">
		/// The message.
		/// </param>
		/// <param name="connectionId">
		/// The connection id.
		/// </param>
		public static void WriteDebug(string message, int connectionId)
		{
			if (log.IsDebugEnabled)
			{
				log.Debug(string.Format("[{0}] - {1}", connectionId, message));
			}

			Debug.WriteLine(string.Format("[{0}] - {1}", connectionId, message));
		}

		/// <summary>
		/// The write error.
		/// </summary>
		/// <param name="message">
		/// The message.
		/// </param>
		/// <param name="e">
		/// The e.
		/// </param>
		public static void WriteError(string message, Exception e)
		{
			if (log.IsErrorEnabled)
			{
				log.Error(string.Format("- {0}", message), e);
			}
		}

		/// <summary>
		/// The write error.
		/// </summary>
		/// <param name="message">
		/// The message.
		/// </param>
		/// <param name="e">
		/// The e.
		/// </param>
		/// <param name="connectionId">
		/// The connection id.
		/// </param>
		public static void WriteError(string message, Exception e, int connectionId)
		{
			if (log.IsErrorEnabled)
			{
				log.Error(string.Format("[{0}] - {1}", connectionId, message), e);
			}
		}

		/// <summary>
		/// The write warning.
		/// </summary>
		/// <param name="message">
		/// The message.
		/// </param>
		public static void WriteWarning(string message)
		{
			if (log.IsWarnEnabled)
			{
				log.Warn(string.Format("- {0}", message));
			}
		}

		/// <summary>
		/// The write warning.
		/// </summary>
		/// <param name="message">
		/// The message.
		/// </param>
		/// <param name="connectionId">
		/// The connection id.
		/// </param>
		public static void WriteWarning(string message, int connectionId)
		{
			if (log.IsWarnEnabled)
			{
				log.Warn(string.Format("[{0}] - {1}", connectionId, message));
			}
		}

		#endregion
	}
}