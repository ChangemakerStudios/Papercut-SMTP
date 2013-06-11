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

namespace Papercut.UI
{
	#region Using

    using System.IO;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows;

    using Papercut.Properties;
    using Papercut.SMTP;

    #endregion

	/// <summary>
	/// Interaction logic for ForwardWindow.xaml
	/// </summary>
	public partial class ForwardWindow : Window
	{
		#region Constants and Fields

		/// <summary>
		/// The email regex.
		/// </summary>
		private static readonly Regex emailRegex = new Regex(
			@"(\A(\s*)\Z)|(\A([^@\s]+)@((?:[-a-z0-9]+\.)+[a-z]{2,})\Z)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		/// <summary>
		/// The message filename.
		/// </summary>
		private readonly string messageFilename;

		/// <summary>
		/// The worker.
		/// </summary>
		private Task worker;

		/// <summary>
		/// The working.
		/// </summary>
		private bool working;

		#endregion

		#region Constructors and Destructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ForwardWindow"/> class.
		/// </summary>
		/// <param name="filename">
		/// The filename.
		/// </param>
		public ForwardWindow(string filename)
			: this()
		{
			this.messageFilename = filename;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ForwardWindow"/> class.
		/// </summary>
		public ForwardWindow()
		{
			this.InitializeComponent();

			// Load previous settings
			this.server.Text = Settings.Default.ForwardServer;
			this.to.Text = Settings.Default.ForwardTo;
			this.@from.Text = Settings.Default.ForwardFrom;

			this.server.Focus();
		}

		#endregion

		#region Methods

		/// <summary>
		/// The cancel button_ click.
		/// </summary>
		/// <param name="sender">
		/// The sender.
		/// </param>
		/// <param name="e">
		/// The e.
		/// </param>
		private void cancelButton_Click(object sender, RoutedEventArgs e)
		{
			if (this.working)
			{
				this.worker.Dispose();
				this.sendingLabel.Visibility = Visibility.Hidden;
			}

			this.DialogResult = false;
		}

		/// <summary>
		/// The send button_ click.
		/// </summary>
		/// <param name="sender">
		/// The sender.
		/// </param>
		/// <param name="e">
		/// The e.
		/// </param>
		private void sendButton_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(this.server.Text) || string.IsNullOrEmpty(this.@from.Text)
					|| string.IsNullOrEmpty(this.to.Text))
			{
				MessageBox.Show(
					"All the text boxes are required, fill them in please.", "Papercut", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (!emailRegex.IsMatch(this.@from.Text) || !emailRegex.IsMatch(this.to.Text))
			{
				MessageBox.Show(
					"You need to enter valid email addresses.", "Papercut", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			var session = new SmtpSession { Sender = this.server.Text.Trim(), MailFrom = this.@from.Text };
			session.Recipients.Add(this.to.Text);
			session.Message = File.ReadAllBytes(this.messageFilename);

			this.worker = Task.Factory.StartNew(
				() =>
					{
						using (var client = new SmtpClient(session))
						{
							client.Send();
						}
					});

			this.worker.ContinueWith(
				(t) =>
					{
						// Save settings for the next time
						Settings.Default.ForwardServer = this.server.Text;
						Settings.Default.ForwardTo = this.to.Text;
						Settings.Default.ForwardFrom = this.@from.Text;
						Settings.Default.Save();

						this.working = false;
						this.sendingLabel.Visibility = Visibility.Hidden;
						this.DialogResult = true;
					},
				TaskScheduler.FromCurrentSynchronizationContext());

			this.working = true;
			this.sendButton.IsEnabled = false;
			this.sendingLabel.Visibility = Visibility.Visible;
		}

		#endregion
	}
}