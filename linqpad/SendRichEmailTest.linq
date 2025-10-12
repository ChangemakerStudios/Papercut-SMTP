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
		MailAddress from = new MailAddress("noreply@company.com", Company.Name());
		MailAddress to = new MailAddress("customer@example.com", Name.NameWithMiddle());
		MailMessage myMail = new System.Net.Mail.MailMessage(from, to);

		// add ReplyTo
		MailAddress replyTo = new MailAddress("support@company.com");
		myMail.ReplyToList.Add(replyTo);

		// set subject and encoding
		myMail.Subject = "Welcome to " + Company.Name() + " - Your Account is Ready!";
		myMail.SubjectEncoding = System.Text.Encoding.UTF8;

		// Rich HTML email template
		var htmlBody = @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Welcome Email</title>
    <style>
        body {
            margin: 0;
            padding: 0;
            font-family: Arial, Helvetica, sans-serif;
            background-color: #f4f4f4;
        }
        .email-container {
            max-width: 600px;
            margin: 20px auto;
            background-color: #ffffff;
            border-radius: 8px;
            overflow: hidden;
            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
        }
        .header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 40px 20px;
            text-align: center;
            color: white;
        }
        .header h1 {
            margin: 0;
            font-size: 28px;
            font-weight: 700;
        }
        .content {
            padding: 40px 30px;
        }
        .content h2 {
            color: #333333;
            font-size: 24px;
            margin-top: 0;
        }
        .content p {
            color: #666666;
            font-size: 16px;
            line-height: 1.6;
        }
        .button {
            display: inline-block;
            padding: 14px 32px;
            margin: 20px 0;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            text-decoration: none;
            border-radius: 5px;
            font-weight: bold;
            font-size: 16px;
        }
        .features {
            background-color: #f8f9fa;
            padding: 30px;
            margin: 20px 0;
            border-radius: 8px;
        }
        .feature-item {
            margin: 15px 0;
            padding-left: 25px;
            position: relative;
        }
        .feature-item:before {
            content: '✓';
            position: absolute;
            left: 0;
            color: #667eea;
            font-weight: bold;
            font-size: 18px;
        }
        .footer {
            background-color: #f8f9fa;
            padding: 30px;
            text-align: center;
            color: #999999;
            font-size: 14px;
        }
        .social-links {
            margin: 20px 0;
        }
        .social-links a {
            display: inline-block;
            margin: 0 10px;
            color: #667eea;
            text-decoration: none;
        }
        .divider {
            height: 1px;
            background-color: #e0e0e0;
            margin: 30px 0;
        }
        @media only screen and (max-width: 600px) {
            .email-container {
                margin: 10px;
            }
            .content {
                padding: 20px 15px;
            }
            .header h1 {
                font-size: 24px;
            }
        }
    </style>
</head>
<body>
    <div class=""email-container"">
        <!-- Header -->
        <div class=""header"">
            <h1>🎉 Welcome to " + Company.Name() + @"!</h1>
        </div>

        <!-- Main Content -->
        <div class=""content"">
            <h2>Hello " + Name.FirstName() + @",</h2>

            <p>We're thrilled to have you on board! Your account has been successfully created and you're ready to explore all the amazing features we have to offer.</p>

            <p>Here's what you can do next:</p>

            <div class=""features"">
                <div class=""feature-item"">Complete your profile to personalize your experience</div>
                <div class=""feature-item"">Connect with other users in your network</div>
                <div class=""feature-item"">Explore our comprehensive dashboard</div>
                <div class=""feature-item"">Set up your preferences and notifications</div>
            </div>

            <center>
                <a href=""https://localhost:5000/dashboard"" class=""button"">Get Started Now</a>
            </center>

            <div class=""divider""></div>

            <p><strong>Quick Stats:</strong></p>
            <ul>
                <li>Account ID: <code>" + Guid.NewGuid().ToString().Substring(0, 8) + @"</code></li>
                <li>Member since: " + DateTime.Now.ToString("MMMM dd, yyyy") + @"</li>
                <li>Plan: Professional</li>
            </ul>

            <p>If you have any questions, our support team is here to help 24/7. Just reply to this email or visit our help center.</p>

            <p>Best regards,<br>
            <strong>The " + Company.Name() + @" Team</strong></p>
        </div>

        <!-- Footer -->
        <div class=""footer"">
            <div class=""social-links"">
                <a href=""#"">Twitter</a> |
                <a href=""#"">Facebook</a> |
                <a href=""#"">LinkedIn</a>
            </div>

            <p>&copy; 2025 " + Company.Name() + @". All rights reserved.</p>

            <p style=""font-size: 12px; color: #aaaaaa;"">
                " + Address.FullAddress() + @"<br>
                You're receiving this email because you signed up for an account.<br>
                <a href=""#"" style=""color: #667eea;"">Unsubscribe</a> | <a href=""#"" style=""color: #667eea;"">Manage Preferences</a>
            </p>
        </div>
    </div>
</body>
</html>";

		myMail.Body = htmlBody;
		myMail.BodyEncoding = System.Text.Encoding.UTF8;
		myMail.IsBodyHtml = true;
		myMail.Priority = MailPriority.Normal;

		mySmtpClient.Send(myMail);

		Console.WriteLine("Rich email sent successfully!");
		Console.WriteLine($"Subject: {myMail.Subject}");
		Console.WriteLine($"To: {to.DisplayName} <{to.Address}>");
	}
}

// Define other methods, classes and namespaces here
