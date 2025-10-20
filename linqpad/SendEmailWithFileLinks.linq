<Query Kind="Program">
  <Namespace>System.Net.Mail</Namespace>
  <Namespace>System.Net.Mime</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
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
		MailAddress from = new MailAddress("test@company.com", "Test Sender");
		MailAddress to = new MailAddress("user@example.com", "Test User");
		MailMessage myMail = new System.Net.Mail.MailMessage(from, to);

		// set subject and encoding
		myMail.Subject = "Test Email with file:/// Links (Issue #232)";
		myMail.SubjectEncoding = System.Text.Encoding.UTF8;

		// HTML email with file:/// links
		var htmlBody = @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>File Links Test</title>
    <style>
        body {
            font-family: Arial, Helvetica, sans-serif;
            margin: 20px;
            line-height: 1.6;
        }
        h1 {
            color: #333;
        }
        .test-section {
            margin: 20px 0;
            padding: 15px;
            border-left: 4px solid #667eea;
            background-color: #f8f9fa;
        }
        a {
            color: #667eea;
            text-decoration: none;
            font-weight: bold;
        }
        a:hover {
            text-decoration: underline;
        }
        code {
            background-color: #e9ecef;
            padding: 2px 6px;
            border-radius: 3px;
            font-family: 'Courier New', monospace;
        }
    </style>
</head>
<body>
    <h1>🔗 Testing file:/// Links</h1>

    <p>This email tests the functionality of <code>file:///</code> links in Papercut SMTP (Issue #232).</p>

    <div class=""test-section"">
        <h2>Test 1: Link to a Directory</h2>
        <p>Click this link to open your Windows directory in Explorer:</p>
        <p><a href=""file:///C:/Windows"">📁 Open C:\Windows Directory</a></p>
    </div>

    <div class=""test-section"">
        <h2>Test 2: Link to a File</h2>
        <p>Click this link to open notepad.exe with its associated application:</p>
        <p><a href=""file:///C:/Windows/System32/notepad.exe"">📄 Open Notepad</a></p>
    </div>

    <div class=""test-section"">
        <h2>Test 3: Link to a Text File</h2>
        <p>Click this link to open a common Windows text file:</p>
        <p><a href=""file:///C:/Windows/system.ini"">📝 Open system.ini</a></p>
    </div>

    <div class=""test-section"">
        <h2>Test 4: Link to User Profile Directory</h2>
        <p>Click this link to open your user profile directory:</p>
        <p><a href=""file:///" + Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Replace("\\", "/") + @""">👤 Open User Profile</a></p>
    </div>

    <div class=""test-section"">
        <h2>Expected Behavior</h2>
        <ul>
            <li>✅ Directory links should open in Windows Explorer</li>
            <li>✅ File links should open with their associated application</li>
            <li>❌ Links should NOT be treated as downloads in the WebView2 browser</li>
        </ul>
    </div>

    <hr>
    <p style=""color: #666; font-size: 14px;"">
        This test email was generated for
        <a href=""https://github.com/ChangemakerStudios/Papercut-SMTP/issues/232"">Issue #232</a> -
        Make file links functional
    </p>
</body>
</html>";

		myMail.Body = htmlBody;
		myMail.BodyEncoding = System.Text.Encoding.UTF8;
		myMail.IsBodyHtml = true;
		myMail.Priority = MailPriority.Normal;

		mySmtpClient.Send(myMail);

		Console.WriteLine("Test email sent successfully!");
		Console.WriteLine($"Subject: {myMail.Subject}");
		Console.WriteLine($"To: {to.DisplayName} <{to.Address}>");
		Console.WriteLine("\nOpen this email in Papercut and click the file:/// links to test the feature.");
	}
}
