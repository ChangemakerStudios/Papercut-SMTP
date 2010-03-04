using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Net.Mail;
using Net.Mime;
using Papercut.Properties;
using Papercut.Smtp;
using Application=System.Windows.Application;
using ContextMenu=System.Windows.Forms.ContextMenu;
using DataFormats=System.Windows.DataFormats;
using DataObject=System.Windows.DataObject;
using DragDropEffects=System.Windows.DragDropEffects;
using ListBox=System.Windows.Controls.ListBox;
using MenuItem=System.Windows.Forms.MenuItem;
using Point=System.Windows.Point;

namespace Papercut
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private delegate void MessageNotificationDelegate(MessageEntry entry);

		private NotifyIcon notification;
		private Server server;

		public MainWindow()
		{
			InitializeComponent();

			// Set up the notification icon
			notification = new NotifyIcon();
			notification.Icon = new Icon(Application.GetResourceStream(new Uri("/Papercut;component/App.ico", UriKind.Relative)).Stream);
			notification.Text = "Papercut";
			notification.Visible = true;
			notification.DoubleClick +=
				delegate
				{
					Show();
					WindowState = WindowState.Normal;
					Topmost = true;
					Focus();
					Topmost = false;
				};
			notification.BalloonTipClicked +=
				delegate
				{
					Show();
					WindowState = WindowState.Normal;
					messagesList.SelectedIndex = messagesList.Items.Count - 1;
				};
			notification.ContextMenu = new ContextMenu(
				new[]
					{
						new MenuItem("Show",
											delegate
												{
													Show();
													WindowState = WindowState.Normal;
													Focus();
												}),
						new MenuItem("Exit", delegate { Close(); })
					}
				);

			// Set the version label
			versionLabel.Content = string.Format("Papercut v{0}", Assembly.GetExecutingAssembly().GetName().Version.ToString(3));

			// Load existing messages
			LoadMessages();
			messagesList.Items.SortDescriptions.Add(new SortDescription("ModifiedDate", ListSortDirection.Ascending));

			// Begin listening for new messages
			Processor.MessageReceived += new EventHandler<MessageEventArgs>(Processor_MessageReceived);

			// Start listening for connections
			server = new Server();
			try
			{
				server.Start();
			}
			catch
			{
				System.Windows.MessageBox.Show("Failed to bind to the address/port specified.  The port may already be in use by another process.  Please change the configuration in the Options dialog.", "Operation Failure");
			}

			// Minimize if set to
			if (Settings.Default.StartMinimized)
				Hide();
		}

		#region Helper Methods

		/// <summary>
		/// Load existing messages from the file system
		/// </summary>
		private void LoadMessages()
		{
			string[] files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.eml");

			foreach (string file in files)
			{
				MessageEntry entry = new MessageEntry(file);
				messagesList.Items.Add(entry);
			}
		}

		/// <summary>
		/// Add a newly received message and show the balloon notification
		/// </summary>
		private void AddNewMessage(MessageEntry entry)
		{
			// Add it to the list box
			messagesList.Items.Add(entry);

			// Show the notification
			notification.ShowBalloonTip(5000, "", "New message received!", ToolTipIcon.Info);
		}

		/// <summary>
		/// Write the HTML to a temporary file and render it to the HTML view
		/// </summary>
        private void SetBrowserDocument(MailMessageEx mailMessageEx)
		{
			//double d = new Random().NextDouble();
		    string tempPath = Path.GetTempPath();

            string htmlFile = Path.Combine(tempPath, "papercut.htm");

            string htmlText = mailMessageEx.Body;
            foreach (var attachment in mailMessageEx.Attachments)
            {
                if ((!string.IsNullOrEmpty(attachment.ContentId)) &&
                    (attachment.ContentStream != null))
                {
                    string fileName = Path.Combine(tempPath, attachment.ContentId);

                    using (var fs = File.OpenWrite(fileName))
                    {
                        int Length = 256;
                        Byte[] buffer = new Byte[Length];
                        int bytesRead = attachment.ContentStream.Read(buffer, 0, Length);
                        // write the required bytes
                        while (bytesRead > 0)
                        {
                            fs.Write(buffer, 0, bytesRead);
                            bytesRead = attachment.ContentStream.Read(buffer, 0, Length);
                        }
                        fs.Close();
                    }

                    htmlText = 
                        htmlText
                            .Replace(String.Format("cid:{0}", attachment.ContentId), attachment.ContentId)
                            .Replace(String.Format("cid:'{0}'", attachment.ContentId), attachment.ContentId)
                            .Replace(String.Format("cid:\"{0}\"", attachment.ContentId), attachment.ContentId);
                }
            } 
            
            using (TextWriter f = new StreamWriter(htmlFile))
                f.Write(htmlText);

		    htmlView.Navigate(new Uri(htmlFile));
			htmlView.Refresh();
		}

		#endregion

		#region Event Handlers

		private void messagesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// If there are no selected items, then disable the Delete button, clear the boxes, and return
			if (e.AddedItems.Count == 0)
			{
				deleteButton.IsEnabled = false;
				forwardButton.IsEnabled = false;
				rawView.Text = "";
				bodyView.Text = "";
				htmlViewTab.Visibility = Visibility.Hidden;
				tabControl.SelectedIndex = 0;
				return;
			}

			// Enable the delete and forward button
			deleteButton.IsEnabled = true;
			forwardButton.IsEnabled = true;

			// Load the file as an array of lines
			List<string> lines = new List<string>();
			string line;
			using (StreamReader sr = new StreamReader(((MessageEntry)e.AddedItems[0]).File))
				while ((line = sr.ReadLine()) != null)
					lines.Add(line);
			string[] linesArray = lines.ToArray();

			// Set the raw message view
			rawView.Text = string.Join("\n", linesArray);

			try
			{
				// Load the MIME body
				MimeReader mr = new MimeReader(linesArray);
				MimeEntity me = mr.CreateMimeEntity();
				MailMessageEx mme = me.ToMailMessageEx();
				bodyView.Text = mme.Body;
				bodyViewTab.Visibility = Visibility.Visible;

				// If it is HTML, render it to the HTML view
				if (mme.IsBodyHtml)
				{
					SetBrowserDocument(mme);
					htmlViewTab.Visibility = Visibility.Visible;
				}
				else
				{
					htmlViewTab.Visibility = Visibility.Hidden;
					if (tabControl.SelectedItem == htmlViewTab)
						tabControl.SelectedIndex = 1;
				}
			}
			catch
			{
				bodyViewTab.Visibility = Visibility.Hidden;
				htmlViewTab.Visibility = Visibility.Hidden;
				tabControl.SelectedIndex = 0;
			}
		}

		private readonly object deleteLockObject = new object();

		private void deleteButton_Click(object sender, RoutedEventArgs e)
		{
			// Lock to prevent rapid clicking issues
			lock (deleteLockObject)
			{
				Array messages = new MessageEntry[messagesList.SelectedItems.Count];
				messagesList.SelectedItems.CopyTo(messages, 0);

				// Capture index position first
				int index = messagesList.SelectedIndex;

				foreach (MessageEntry entry in messages)
				{
					// Delete the file and remove the entry
					if (File.Exists(entry.File))
						File.Delete(entry.File);
					messagesList.Items.Remove(entry);
				}

				// If there are more than the index location, keep the same position in the list
				if (messagesList.Items.Count > index)
					messagesList.SelectedIndex = index;
				// If there are fewer, move to the last one
				else if (messagesList.Items.Count > 0)
					messagesList.SelectedIndex = messagesList.Items.Count - 1;
			}
		}

		void forwardButton_Click(object sender, RoutedEventArgs e)
		{
			MessageEntry entry = messagesList.SelectedItem as MessageEntry;
			ForwardWindow fw = new ForwardWindow(entry.File);
			fw.Owner = this;
			fw.ShowDialog();
		}

		void Processor_MessageReceived(object sender, MessageEventArgs e)
		{
			// This takes place on a background thread from the SMTP server
			// Dispatch it back to the main thread for the update
			Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new MessageNotificationDelegate(AddNewMessage), e.Entry);
		}

		protected override void OnStateChanged(EventArgs e)
		{
			// Hide the window if minimized so it doesn't show up on the task bar
			if (WindowState == WindowState.Minimized)
				Hide();
			base.OnStateChanged(e);
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			notification.Dispose();
			server.Stop();
		}

		private void Options_Click(object sender, RoutedEventArgs e)
		{
			OptionsWindow ow = new OptionsWindow();
			ow.Owner = this;
			ow.ShowInTaskbar = false;
			if (ow.ShowDialog().Value)
			{
				try
				{
					// Force the server to rebind
					server.Bind();
				}
				catch(Exception ex)
				{
					System.Windows.MessageBox.Show("Failed to rebind to the address/port specified.  The port may already be in use by another process.  Please update the configuration.", "Operation Failure");
					Options_Click(null, null);
				}
			}
		}

		private void Exit_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void GoToSite(object sender, MouseButtonEventArgs e)
		{
			Process.Start("http://papercut.codeplex.com/");
		}

		#endregion

		#region Drag & Drop Handling

		private void MouseDownHandler(object sender, MouseButtonEventArgs e)
		{
			return;
            /*
			ListBox parent = (ListBox)sender;

			// Get the object source for the selected item
			string data = GetObjectDataFromPoint(parent, e.GetPosition(parent));

			// If the data is not null then start the drag drop operation
			if (data != null)
			{
				DataObject doo = new DataObject(DataFormats.FileDrop, new[] { data });
				DragDrop.DoDragDrop(parent, doo, DragDropEffects.Copy);
			}
            */
		}

		private static string GetObjectDataFromPoint(ListBox source, Point point)
		{
			UIElement element = source.InputHitTest(point) as UIElement;
			if (element != null)
			{
				// Get the object from the element
				object data = DependencyProperty.UnsetValue;
				while (data == DependencyProperty.UnsetValue)
				{
					// Try to get the object value for the corresponding element
					data = source.ItemContainerGenerator.ItemFromContainer(element);

					// Get the parent and we will iterate again
					if (data == DependencyProperty.UnsetValue)
						element = VisualTreeHelper.GetParent(element) as UIElement;

					// If we reach the actual listbox then we must break to avoid an infinite loop
					if (element == source)
						return null;
				}

				// Return the data that we fetched only if it is not Unset value, 
				// which would mean that we did not find the data
				if (data is MessageEntry)
					return ((MessageEntry)data).File;
			}

			return null;
		}

		#endregion

	}
}