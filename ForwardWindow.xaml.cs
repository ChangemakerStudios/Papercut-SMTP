using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using Papercut.Smtp;

namespace Papercut
{
	/// <summary>
	/// Interaction logic for ForwardWindow.xaml
	/// </summary>
	public partial class ForwardWindow : Window
	{
		string messageFilename;
		BackgroundWorker worker;
		bool working = false;
		static readonly Regex emailRegex = new Regex(@"(\A(\s*)\Z)|(\A([^@\s]+)@((?:[-a-z0-9]+\.)+[a-z]{2,})\Z)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		public ForwardWindow(string filename) : this()
		{
			messageFilename = filename;
		}

		public ForwardWindow()
		{
			InitializeComponent();
		}

		void cancelButton_Click(object sender, RoutedEventArgs e)
		{
			if (working)
			{
				worker.CancelAsync();
				sendingLabel.Visibility = Visibility.Hidden;
			}
			DialogResult = false;
		}

		void sendButton_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(server.Text) || string.IsNullOrEmpty(from.Text) || string.IsNullOrEmpty(to.Text))
			{
				MessageBox.Show("All the text boxes are required, fill them in please.", "Papercut", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if(!emailRegex.IsMatch(from.Text) || !emailRegex.IsMatch(to.Text))
			{
				MessageBox.Show("You need to enter valid email addresses.", "Papercut", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			SmtpSession session = new SmtpSession();
			session.Sender = server.Text.Trim();
			session.MailFrom = from.Text;
			session.Recipients.Add(to.Text);
			session.Message = File.ReadAllBytes(messageFilename);

			worker = new BackgroundWorker();

			worker.DoWork += delegate(object s, DoWorkEventArgs args)
			{
				SmtpSession _session = args.Argument as SmtpSession;
				SmtpClient client = new SmtpClient(_session);
				client.Send();
			};

			worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
			{
				working = false;
				sendingLabel.Visibility = Visibility.Hidden;
				DialogResult = true;
			};

			working = true;
			sendButton.IsEnabled = false;
			sendingLabel.Visibility = Visibility.Visible;
			worker.RunWorkerAsync(session);
		}
	}
}
