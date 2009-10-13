using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Papercut.Smtp
{
	static class Util
	{
		public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> act)
		{
			foreach (T element in source) act(element);
			return source;
		}

		public static string GetIPAddress()
		{
			IPAddress ip = GetExternalIp();
			if (ip == null)
				return Dns.GetHostEntry(Dns.GetHostName()).HostName;
			return Dns.GetHostEntry(ip).HostName;
		}

		private static IPAddress GetExternalIp()
		{
			try
			{
				string whatIsMyIp = "http://www.whatismyip.com/automation/n09230945.asp";
				WebClient wc = new WebClient();
				string requestHtml = Encoding.UTF8.GetString(wc.DownloadData(whatIsMyIp));
				return IPAddress.Parse(requestHtml);
			}
			catch
			{
				return null;
			}
		}
	}
}
