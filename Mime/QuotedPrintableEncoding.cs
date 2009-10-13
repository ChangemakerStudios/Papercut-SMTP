using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Net.Mime
{
	/// <summary>
	/// This class is based on the QuotedPrintable class written by Bill Gearhart
	/// found at http://www.aspemporium.com/classes.aspx?cid=6
	/// </summary>
	public static class QuotedPrintableEncoding
	{
		public static string Decode(string encoded)
		{
			if (encoded == null)
				throw new ArgumentNullException();

			string line;
			StringWriter sw = new StringWriter();
			StringReader sr = new StringReader(encoded);
			try
			{
				while ((line = sr.ReadLine()) != null)
				{
					if (line.EndsWith("="))
						sw.Write(HexDecoder(line.Substring(0, line.Length - 1)));
					else
						sw.WriteLine(HexDecoder(line));

					sw.Flush();
				}
				return sw.ToString();
			}
			finally
			{
				sw.Close();
				sr.Close();
				sw = null;
				sr = null;
			}
		}

		static string HexDecoder(string line)
		{
			if (line == null)
				throw new ArgumentNullException();

			//parse looking for =XX where XX is hexadecimal
			Regex re = new Regex(
				"(\\=([0-9A-F][0-9A-F]))",
				RegexOptions.IgnoreCase
			);
			return re.Replace(line, new MatchEvaluator(HexDecoderEvaluator));
		}

		static string HexDecoderEvaluator(Match m)
		{
			string hex = m.Groups[2].Value;
			int iHex = Convert.ToInt32(hex, 16);
			char c = (char)iHex;
			return c.ToString();
		}
	}
}