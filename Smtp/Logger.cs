using System;
using System.Diagnostics;
using System.Reflection;
using log4net;
[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace Papercut.Smtp
{
	/// <summary>
	/// Summary description for Logger.
	/// </summary>
	public class Logger
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public static void Write(string message)
		{
			if (log.IsInfoEnabled)
				log.Info(string.Format("- {0}", message));
		}

		public static void Write(string message, int connectionId)
		{
			if (log.IsInfoEnabled)
				log.Info(string.Format("[{0}] - {1}", connectionId, message));
		}

		public static void WriteDebug(string message)
		{
			if (log.IsDebugEnabled)
				log.Debug(string.Format("- {0}", message));
			Debug.WriteLine(message);
		}

		public static void WriteDebug(string message, int connectionId)
		{
			if (log.IsDebugEnabled)
				log.Debug(string.Format("[{0}] - {1}", connectionId, message));
			Debug.WriteLine(string.Format("[{0}] - {1}", connectionId, message));
		}

		public static void WriteError(string message, Exception e)
		{
			if (log.IsErrorEnabled)
				log.Error(string.Format("- {0}", message), e);
		}

		public static void WriteError(string message, Exception e, int connectionId)
		{
			if (log.IsErrorEnabled)
				log.Error(string.Format("[{0}] - {1}", connectionId, message), e);
		}

		public static void WriteWarning(string message)
		{
			if (log.IsWarnEnabled)
				log.Warn(string.Format("- {0}", message));
		}

		public static void WriteWarning(string message, int connectionId)
		{
			if (log.IsWarnEnabled)
				log.Warn(string.Format("[{0}] - {1}", connectionId, message));
		}

	}
}