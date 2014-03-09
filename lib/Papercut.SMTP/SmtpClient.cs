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

namespace Papercut.SMTP
{
	#region Using

	using System.Net.Mail;
	using System.Net.Sockets;
	using System.Text;

	#endregion

	/// <summary>
	/// The smtp client.
	/// </summary>
	public class SmtpClient : TcpClient
	{
		#region Constants and Fields

		/// <summary>
		/// The session.
		/// </summary>
		private readonly SmtpSession session;

		#endregion

		#region Constructors and Destructors

		/// <summary>
		/// Initializes a new instance of the <see cref="SmtpClient"/> class.
		/// </summary>
		/// <param name="session">
		/// The session.
		/// </param>
		public SmtpClient(SmtpSession session)
		{
			this.session = session;
		}

		#endregion

		#region Public Methods and Operators

		/// <summary>
		/// The send.
		/// </summary>
		/// <exception cref="SmtpException">
		/// </exception>
		public void Send()
		{
			string response;

			this.Connect(this.session.Sender, 25);
			response = this.Response();
			if (response.Substring(0, 3) != "220")
			{
				throw new SmtpException(response);
			}

			this.Write("HELO {0}\r\n", Util.GetIPAddress());
			response = this.Response();
			if (response.Substring(0, 3) != "250")
			{
				throw new SmtpException(response);
			}

			this.Write("MAIL FROM:<{0}>\r\n", this.session.MailFrom);
			response = this.Response();
			if (response.Substring(0, 3) != "250")
			{
				throw new SmtpException(response);
			}

			this.session.Recipients.ForEach(
				address =>
					{
						this.Write("RCPT TO:<{0}>\r\n", address);
						response = this.Response();
						if (response.Substring(0, 3) != "250")
						{
							throw new SmtpException(response);
						}
					});

			this.Write("DATA\r\n");
			response = this.Response();
			if (response.Substring(0, 3) != "354")
			{
				throw new SmtpException(response);
			}

			NetworkStream stream = this.GetStream();
			stream.Write(this.session.Message, 0, this.session.Message.Length);

			this.Write("\r\n.\r\n");
			response = this.Response();
			if (response.Substring(0, 3) != "250")
			{
				throw new SmtpException(response);
			}

			this.Write("QUIT\r\n");
			response = this.Response();
			if (response.IndexOf("221") == -1)
			{
				throw new SmtpException(response);
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// The response.
		/// </summary>
		/// <returns>
		/// The response.
		/// </returns>
		private string Response()
		{
			var enc = new ASCIIEncoding();
			var serverbuff = new byte[1024];
			NetworkStream stream = this.GetStream();
			int count = stream.Read(serverbuff, 0, 1024);

			return count == 0 ? string.Empty : enc.GetString(serverbuff, 0, count);
		}

		/// <summary>
		/// The write.
		/// </summary>
		/// <param name="format">
		/// The format.
		/// </param>
		/// <param name="args">
		/// The args.
		/// </param>
		private void Write(string format, params object[] args)
		{
			var en = new ASCIIEncoding();

		    byte[] writeBuffer = en.GetBytes(string.Format(format, args));
			NetworkStream stream = this.GetStream();
			stream.Write(writeBuffer, 0, writeBuffer.Length);
		}

		#endregion
	}
}