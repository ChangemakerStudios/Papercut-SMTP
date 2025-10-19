<Query Kind="Program">
  <NuGetReference>RimuTec.Faker</NuGetReference>
  <Namespace>System.Net.Mail</Namespace>
  <Namespace>System.Net.Mime</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>RimuTec.Faker</Namespace>
</Query>

void Main()
{
	using (SmtpClient mySmtpClient = new SmtpClient("127.0.0.1"))
	{
		// set smtp-client with basicAuthentication
		mySmtpClient.UseDefaultCredentials = false;
		System.Net.NetworkCredential basicAuthenticationInfo = new
		   System.Net.NetworkCredential("username", "password");
		mySmtpClient.Credentials = basicAuthenticationInfo;

		// add from,to mailaddresses
		MailAddress from = new MailAddress(Name.FirstName()+"@example.com", Name.NameWithMiddle());
		MailAddress to = new MailAddress("you@example.com", "you");
		MailMessage myMail = new System.Net.Mail.MailMessage(from, to);

		// add ReplyTo
		MailAddress replyTo = new MailAddress("reply@example.com");
		myMail.ReplyToList.Add(replyTo);

		// set subject and encoding
		myMail.Subject = "SSL Certificate Test - " + Guid.NewGuid().ToString();
		myMail.SubjectEncoding = System.Text.Encoding.UTF8;

		// set body-message with PNG images from badssl.com (expired, wrong host, self-signed, untrusted root)
		var htmlBody = @"<html>
<body>
	<h2>SSL Certificate Error Test Email</h2>
	<p>This email contains PNG images from various bad SSL certificate scenarios from badssl.com</p>

	<h3>1. Expired Certificate</h3>
	<img src=""https://expired.badssl.com/icons/icon-red.png"" alt=""Expired"" style=""width: 100px; border: 2px solid red;"" />

	<h3>2. Wrong Host Certificate</h3>
	<img src=""https://wrong.host.badssl.com/icons/icon-orange.png"" alt=""Wrong Host"" style=""width: 100px; border: 2px solid orange;"" />

	<h3>3. Self-Signed Certificate</h3>
	<img src=""https://self-signed.badssl.com/icons/icon-yellow.png"" alt=""Self Signed"" style=""width: 100px; border: 2px solid yellow;"" />

	<h3>4. Untrusted Root Certificate</h3>
	<img src=""https://untrusted-root.badssl.com/icons/icon-red.png"" alt=""Untrusted Root"" style=""width: 100px; border: 2px solid purple;"" />

	<h3>5. Good SSL (for comparison)</h3>
	<img src=""https://badssl.com/icons/icon-green.png"" alt=""Good SSL"" style=""width: 100px; border: 2px solid green;"" />

	<hr />
	<p><b>Expected behavior:</b></p>
	<ul>
		<li>With <i>Ignore SSL Certificate Errors</i> <b>enabled</b>: All 5 images should display</li>
		<li>With <i>Ignore SSL Certificate Errors</i> <b>disabled</b>: Only the last image (Good SSL) should display</li>
	</ul>

	<p>" + string.Join("<br />", Lorem.Paragraphs()) + @"</p>
</body>
</html>";

		myMail.Body = htmlBody;
		myMail.BodyEncoding = System.Text.Encoding.UTF8;
		myMail.IsBodyHtml = true;
		myMail.Priority = MailPriority.Normal;

		mySmtpClient.Send(myMail);

		Console.WriteLine("Test email sent successfully!");
		Console.WriteLine("Check Papercut SMTP to verify SSL certificate handling.");
	}
}

// Define other methods, classes and namespaces here
