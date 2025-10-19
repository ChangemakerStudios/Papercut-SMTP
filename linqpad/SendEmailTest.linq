<Query Kind="Program">
  <NuGetReference>RimuTec.Faker</NuGetReference>
  <Namespace>System.Net.Mail</Namespace>
  <Namespace>System.Net.Mime</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>RimuTec.Faker</Namespace>
</Query>

void Main()
{
	var svg = File.ReadAllBytes(@"C:\Temp\scissors.svg");

	Parallel.For(0, 5, (i) =>
	{
		using (SmtpClient mySmtpClient = new SmtpClient("127.0.0.1"))
		{
			// set smtp-client with basicAuthentication
			mySmtpClient.UseDefaultCredentials = false;
			System.Net.NetworkCredential basicAuthenticationInfo = new
			   System.Net.NetworkCredential("username", "password");
			mySmtpClient.Credentials = basicAuthenticationInfo;

			// add from,to mailaddresses
			MailAddress from = new MailAddress(Name.FirstName()+"@doggy.com", Name.NameWithMiddle());
			MailAddress to = new MailAddress("you@example.com", "you");
			MailMessage myMail = new System.Net.Mail.MailMessage(from, to);

			// add ReplyTo
			MailAddress replyTo = new MailAddress("reply@example.com");
			myMail.ReplyToList.Add(replyTo);

			// set subject and encoding
			myMail.Subject = "Dear " + Name.NameWithMiddle() + " Fun at " + Educator.Campus() + " " + Guid.NewGuid().ToString();
			myMail.SubjectEncoding = System.Text.Encoding.UTF8;

			// set body-message and encoding
			myMail.Body = "<b> " + string.Join("<br />", Lorem.Paragraphs()) + @"</b><br>using <b>HTML</b>. <a href=""http://localhost:5119/weatherforecast"" target=""_blank"">Localhost</a><br/><img src=""cid:image1"" />";
			myMail.BodyEncoding = System.Text.Encoding.UTF8;

			// text or html
			myMail.IsBodyHtml = true;
			myMail.Priority = i % 2 == 0 ? MailPriority.Low : MailPriority.Normal;

			using (var file = new MemoryStream(svg))
			{
				var linkedResource = new LinkedResource(file, new ContentType("image/svg+xml"));
				linkedResource.ContentId = "image1"; // Set ContentId before using it in HTML

				// My mail provider would not accept an email with only an image, adding hello so that the content looks less suspicious.
				var htmlBody = @"<b> " + string.Join("<br />", Lorem.Paragraphs()) + @$"</b>
				<br /><img src=""cid:{linkedResource.ContentId}"" style=""width: 200px"" />";
				var alternateView = AlternateView.CreateAlternateViewFromString(htmlBody, null, MediaTypeNames.Text.Html);
				alternateView.LinkedResources.Add(linkedResource);

				myMail.AlternateViews.Add(alternateView);

				mySmtpClient.Send(myMail);
			}
		}
	});
}

// Define other methods, classes and namespaces here
