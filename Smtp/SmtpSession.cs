using System.Collections.Generic;

namespace Papercut.Smtp
{
	public class SmtpSession
	{
		private string _sender;
		private string _mailFrom;
		private bool _useUtf8;
		private IList<string> _recipients = new List<string>();
		private byte[] _message;

		public void Reset()
		{
			_mailFrom = null;
			_recipients.Clear();
			_useUtf8 = false;
		}

		public string Sender
		{
			get { return _sender; }
			set { _sender = value; }
		}

		public string MailFrom
		{
			get { return _mailFrom; }
			set { _mailFrom = value; }
		}

		public bool UseUtf8
		{
			get { return _useUtf8; }
			set { _useUtf8 = value; }
		}

		public IList<string> Recipients
		{
			get { return _recipients; }
			set { _recipients = value; }
		}

		public byte[] Message
		{
			get { return _message; }
			set { _message = value; }
		}
	}
}