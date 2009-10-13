using System.Net.Mail;
using System.Net.Sockets;

namespace Papercut.Smtp
{
	public class SmtpClient : TcpClient
	{
		SmtpSession session;

		public SmtpClient(SmtpSession session)
		{
			this.session = session;
		}

		public void Send()
		{
			string response;

			Connect(session.Sender, 25);
			response = Response();
			if (response.Substring(0, 3) != "220")
				throw new SmtpException(response);

			Write("HELO {0}\r\n", Util.GetIPAddress());
			response = Response();
			if (response.Substring(0, 3) != "250")
				throw new SmtpException(response);

			Write("MAIL FROM:<{0}>\r\n", session.MailFrom);
			response = Response();
			if (response.Substring(0, 3) != "250")
				throw new SmtpException(response);

			session.Recipients.ForEach(address =>
				{
					Write("RCPT TO:<{0}>\r\n", address);
					response = Response();
					if (response.Substring(0, 3) != "250")
						throw new SmtpException(response);
				});

			Write("DATA\r\n");
			response = Response();
			if (response.Substring(0, 3) != "354")
				throw new SmtpException(response);

			NetworkStream stream = GetStream();
			stream.Write(session.Message, 0, session.Message.Length);

			Write("\r\n.\r\n");
			response = Response();
			if (response.Substring(0, 3) != "250")
				throw new SmtpException(response);

			Write("QUIT\r\n");
			response = Response();
			if (response.IndexOf("221") == -1)
				throw new SmtpException(response);
		}

		void Write(string format, params object[] args)
		{
			System.Text.ASCIIEncoding en = new System.Text.ASCIIEncoding();

			byte[] WriteBuffer = new byte[1024];
			WriteBuffer = en.GetBytes(string.Format(format, args));

			NetworkStream stream = GetStream();
			stream.Write(WriteBuffer, 0, WriteBuffer.Length);
		}

		string Response()
		{
			System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
			byte[] serverbuff = new byte[1024];
			NetworkStream stream = GetStream();
			int count = stream.Read(serverbuff, 0, 1024);
			if (count == 0)
				return "";
			return enc.GetString(serverbuff, 0, count);
		}

	}
}
