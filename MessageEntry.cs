using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Papercut
{
	class MessageEntry
	{
		private FileInfo info;
		private DateTime? created;

		private static readonly Regex nameFormat = new Regex(@"^\d{14,16}\.eml$", RegexOptions.Compiled);

		public MessageEntry(string file)
		{
			info = new FileInfo(file);

			if(nameFormat.IsMatch(info.Name))
				created = DateTime.ParseExact(info.Name.Replace(".eml", ""), "yyyyMMddHHmmssFF", CultureInfo.InvariantCulture);
		}

		public string File
		{
			get { return info.FullName; }
		}

		public string Name
		{
			get { return info.Name; }
		}

		public DateTime ModifiedDate
		{
			get { return info.LastWriteTime; }
		}

		public override string ToString()
		{
			if(created.HasValue)
				return created.Value.ToString("G");
			else
				return info.Name;
		}
	}
}