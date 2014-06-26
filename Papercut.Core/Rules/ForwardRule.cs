namespace Papercut.Core.Rules
{
    using System;

    using Papercut.Core.Message;
    using Papercut.Core.Network;

    [Serializable]
    public class ForwardRule : Rule
    {
        public string FromEmail;

        public string SmtpServer;

        public string ToEmail;

        public ForwardRule(string smtpServer, string fromEmail, string toEmail)
        {
            FromEmail = fromEmail;
            SmtpServer = smtpServer;
            ToEmail = toEmail;
        }
    }
}