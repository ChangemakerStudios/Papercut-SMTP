/*  
 * Papercut
 *
 *  Copyright © 2008 - 2012 Ken Robertson
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

namespace Net.Mime
{
	#region Using

	using System;
	using System.IO;
	using System.Text.RegularExpressions;

	#endregion

	/// <summary>
	/// This class is based on the QuotedPrintable class written by Bill Gearhart found at http://www.aspemporium.com/classes.aspx?cid=6
	/// </summary>
	public static class QuotedPrintableEncoding
	{
		#region Public Methods and Operators

		/// <summary>
		/// The decode.
		/// </summary>
		/// <param name="encoded">
		/// The encoded. 
		/// </param>
		/// <returns>
		/// The decode. 
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// </exception>
		public static string Decode(string encoded)
		{
			if (encoded == null)
			{
				throw new ArgumentNullException();
			}

			string line;
			var sw = new StringWriter();
			var sr = new StringReader(encoded);
			try
			{
				while ((line = sr.ReadLine()) != null)
				{
					if (line.EndsWith("="))
					{
						sw.Write(HexDecoder(line.Substring(0, line.Length - 1)));
					}
					else
					{
						sw.WriteLine(HexDecoder(line));
					}

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

		#endregion

		#region Methods

		/// <summary>
		/// The hex decoder.
		/// </summary>
		/// <param name="line">
		/// The line. 
		/// </param>
		/// <returns>
		/// The hex decoder. 
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// </exception>
		private static string HexDecoder(string line)
		{
			if (line == null)
			{
				throw new ArgumentNullException();
			}

			// parse looking for =XX where XX is hexadecimal
			var re = new Regex("(\\=([0-9A-F][0-9A-F]))", RegexOptions.IgnoreCase);
			return re.Replace(line, HexDecoderEvaluator);
		}

		/// <summary>
		/// The hex decoder evaluator.
		/// </summary>
		/// <param name="m">
		/// The m. 
		/// </param>
		/// <returns>
		/// The hex decoder evaluator. 
		/// </returns>
		private static string HexDecoderEvaluator(Match m)
		{
			string hex = m.Groups[2].Value;
			int iHex = Convert.ToInt32(hex, 16);
			var c = (char)iHex;
			return c.ToString();
		}

		#endregion
	}
}