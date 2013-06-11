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

    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Forms;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Threading;

    using Papercut.Properties;
    using Papercut.SMTP;
    using Papercut.SMTP.Mime;

    using Application = System.Windows.Application;
    using ContextMenu = System.Windows.Forms.ContextMenu;
    using ListBox = System.Windows.Controls.ListBox;
    using MenuItem = System.Windows.Forms.MenuItem;
    using MessageBox = System.Windows.MessageBox;
    using Point = System.Windows.Point;

    #endregion

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Constants and Fields

        /// <summary>
        ///   The delete lock object.
        /// </summary>
        private readonly object deleteLockObject = new object();

        /// <summary>
        ///   The notification.
        /// </summary>
        private readonly NotifyIcon notification;

        /// <summary>
        ///   The server.
        /// </summary>
        private readonly Server server;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class. 
        ///   Initializes a new instance of the <see cref="MainWindow"/> class. Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();

            // Set up the notification icon
            this.notification = new NotifyIcon
                {
                    Icon = new Icon(Application.GetResourceStream(new Uri("/Papercut;component/App.ico", UriKind.Relative)).Stream),
                    Text = "Papercut",
                    Visible = true
                };

            this.notification.Click += delegate
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                    this.Topmost = true;
                    this.Focus();
                    this.Topmost = false;
                };

            this.notification.BalloonTipClicked += delegate
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                    this.messagesList.SelectedIndex = this.messagesList.Items.Count - 1;
                };

            this.notification.ContextMenu = new ContextMenu(new[]
                {
                    new MenuItem("Show",
                                 delegate
                                     {
                                         this.Show();
                                         this.WindowState = WindowState.Normal;
                                         this.Focus();
                                     }) { DefaultItem = true },
                    new MenuItem("Exit", delegate { this.Close(); })
                });

            // Set the version label
            this.versionLabel.Content = string.Format("Papercut v{0}", Assembly.GetExecutingAssembly().GetName().Version.ToString(3));

            // Load existing messages
            this.LoadMessages();
            this.messagesList.Items.SortDescriptions.Add(new SortDescription("ModifiedDate", ListSortDirection.Ascending));

            // Begin listening for new messages
            Processor.MessageReceived += this.Processor_MessageReceived;

            // Start listening for connections
            this.server = new Server();
            try
            {
                this.server.Bind(Settings.Default.IP, Settings.Default.Port);
            }
            catch
            {
                MessageBox.Show(
                    "Failed to bind to the address/port specified.  The port may already be in use by another process.  Please change the configuration in the Options dialog.", 
                    "Operation Failure");
            }

            this.SetTabs();

            UpdateSelectedMessage();

            // Minimize if set to
            if (Settings.Default.StartMinimized)
            {
                this.Hide();
            }
        }

        #endregion

        #region Delegates

        /// <summary>
        /// The message notification delegate.
        /// </summary>
        /// <param name="entry">
        /// The entry. 
        /// </param>
        private delegate void MessageNotificationDelegate(MessageEntry entry);

        #endregion

        #region Methods

        /// <summary>
        /// The on state changed.
        /// </summary>
        /// <param name="e">
        /// The e. 
        /// </param>
        protected override void OnStateChanged(EventArgs e)
        {
            // Hide the window if minimized so it doesn't show up on the task bar
            if (this.WindowState == WindowState.Minimized)
            {
                this.Hide();
            }

            base.OnStateChanged(e);
        }

        /// <summary>
        /// The get object data from point.
        /// </summary>
        /// <param name="source">
        /// The source. 
        /// </param>
        /// <param name="point">
        /// The point. 
        /// </param>
        /// <returns>
        /// The get object data from point. 
        /// </returns>
        private static string GetObjectDataFromPoint(ListBox source, Point point)
        {
            var element = source.InputHitTest(point) as UIElement;
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
                    {
                        element = VisualTreeHelper.GetParent(element) as UIElement;
                    }

                    // If we reach the actual listbox then we must break to avoid an infinite loop
                    if (element == source)
                    {
                        return null;
                    }
                }

                // Return the data that we fetched only if it is not Unset value, 
                // which would mean that we did not find the data
                if (data is MessageEntry)
                {
                    return ((MessageEntry)data).File;
                }
            }

            return null;
        }

        /// <summary>
        /// Add a newly received message and show the balloon notification
        /// </summary>
        /// <param name="entry">
        /// The entry. 
        /// </param>
        private void AddNewMessage(MessageEntry entry)
        {
            // Add it to the list box
            this.messagesList.Items.Add(entry);

            // Show the notification
            this.notification.ShowBalloonTip(5000, string.Empty, "New message received!", ToolTipIcon.Info);
        }

        /// <summary>
        /// The exit_ click.
        /// </summary>
        /// <param name="sender">
        /// The sender. 
        /// </param>
        /// <param name="e">
        /// The e. 
        /// </param>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// The go to site.
        /// </summary>
        /// <param name="sender">
        /// The sender. 
        /// </param>
        /// <param name="e">
        /// The e. 
        /// </param>
        private void GoToSite(object sender, MouseButtonEventArgs e)
        {
            Process.Start("http://papercut.codeplex.com/");
        }

        /// <summary>
        /// Load existing messages from the file system
        /// </summary>
        private void LoadMessages()
        {
            string[] files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.eml");

            foreach (var entry in files.Select(file => new MessageEntry(file)))
            {
                this.messagesList.Items.Add(entry);
            }
        }

        /// <summary>
        /// The mouse down handler.
        /// </summary>
        /// <param name="sender">
        /// The sender. 
        /// </param>
        /// <param name="e">
        /// The e. 
        /// </param>
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

        /// <summary>
        /// The options_ click.
        /// </summary>
        /// <param name="sender">
        /// The sender. 
        /// </param>
        /// <param name="e">
        /// The e. 
        /// </param>
        private void Options_Click(object sender, RoutedEventArgs e)
        {
            var ow = new OptionsWindow { Owner = this, ShowInTaskbar = false };

            var showDialog = ow.ShowDialog();

            if (showDialog == null || !showDialog.Value)
            {
                return;
            }

            try
            {
                // Force the server to rebind
                this.server.Bind(Settings.Default.IP, Settings.Default.Port);
                this.SetTabs();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to rebind to the address/port specified.  The port may already be in use by another process.  Please update the configuration.",
                                "Operation Failure");
                this.Options_Click(null, null);
            }
        }

        /// <summary>
        /// The processor_ message received.
        /// </summary>
        /// <param name="sender">
        /// The sender. 
        /// </param>
        /// <param name="e">
        /// The e. 
        /// </param>
        private void Processor_MessageReceived(object sender, MessageEventArgs e)
        {
            // This takes place on a background thread from the SMTP server
            // Dispatch it back to the main thread for the update
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new MessageNotificationDelegate(this.AddNewMessage), e.Entry);
        }

        /// <summary>
        /// Write the HTML to a temporary file and render it to the HTML view
        /// </summary>
        /// <param name="mailMessageEx">
        /// The mail Message Ex. 
        /// </param>
        private void SetBrowserDocument(MailMessageEx mailMessageEx)
        {
            const int Length = 256;

            // double d = new Random().NextDouble();
            string tempPath = Path.GetTempPath();
            string htmlFile = Path.Combine(tempPath, "papercut.htm");

            string htmlText = mailMessageEx.Body;

            foreach (var attachment in mailMessageEx.Attachments)
            {
                if ((!string.IsNullOrEmpty(attachment.ContentId)) && (attachment.ContentStream != null))
                {
                    string fileName = Path.Combine(tempPath, attachment.ContentId);

                    using (var fs = File.OpenWrite(fileName))
                    {
                        var buffer = new byte[Length];
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
                        htmlText.Replace(string.Format(@"cid:{0}", attachment.ContentId), attachment.ContentId)
                                .Replace(string.Format(@"cid:'{0}'", attachment.ContentId), attachment.ContentId)
                                .Replace(string.Format(@"cid:""{0}""", attachment.ContentId), attachment.ContentId);
                }
            }

            File.WriteAllText(htmlFile, htmlText, Encoding.Unicode);

            this.htmlView.Navigate(new Uri(htmlFile));
            this.htmlView.Refresh();

            this.defaultHtmlView.Navigate(new Uri(htmlFile));
            this.defaultHtmlView.Refresh();
        }

        /// <summary>
        /// The set tabs.
        /// </summary>
        private void SetTabs()
        {
            if (Settings.Default.ShowDefaultTab)
            {
                this.tabControl.SelectedIndex = 0;
                this.defaultTab.Visibility = Visibility.Visible;
            }
            else
            {
                this.tabControl.SelectedIndex = 1;
                this.defaultTab.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// The window_ closing.
        /// </summary>
        /// <param name="sender">
        /// The sender. 
        /// </param>
        /// <param name="e">
        /// The e. 
        /// </param>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            this.notification.Dispose();
            this.server.Stop();
        }

        /// <summary>
        /// The delete button_ click.
        /// </summary>
        /// <param name="sender">
        /// The sender. 
        /// </param>
        /// <param name="e">
        /// The e. 
        /// </param>
        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            // Lock to prevent rapid clicking issues
            lock (this.deleteLockObject)
            {
                Array messages = new MessageEntry[this.messagesList.SelectedItems.Count];
                this.messagesList.SelectedItems.CopyTo(messages, 0);

                // Capture index position first
                int index = this.messagesList.SelectedIndex;

                foreach (MessageEntry entry in messages)
                {
                    // Delete the file and remove the entry
                    if (File.Exists(entry.File))
                    {
                        File.Delete(entry.File);
                    }

                    this.messagesList.Items.Remove(entry);
                }

                this.UpdateSelectedMessage(index);
            }
        }

        private void UpdateSelectedMessage(int? index = null)
        {
            // If there are more than the index location, keep the same position in the list
            if (index.HasValue && this.messagesList.Items.Count > index)
            {
                this.messagesList.SelectedIndex = index.Value;
            }
            else if (this.messagesList.Items.Count > 0)
            {
                // If there are fewer, move to the last one
                this.messagesList.SelectedIndex = this.messagesList.Items.Count - 1;
            }
            else if (this.messagesList.Items.Count == 0)
            {
                this.tabControl.IsEnabled = false;
            }
        }

        /// <summary>
        /// The forward button_ click.
        /// </summary>
        /// <param name="sender">
        /// The sender. 
        /// </param>
        /// <param name="e">
        /// The e. 
        /// </param>
        private void forwardButton_Click(object sender, RoutedEventArgs e)
        {
            var entry = this.messagesList.SelectedItem as MessageEntry;
            if (entry != null)
            {
                var fw = new ForwardWindow(entry.File) { Owner = this };
                fw.ShowDialog();
            }
        }

        private CancellationTokenSource _currentMessageCancellationTokenSource = null;

        /// <summary>
        /// The messages list_ selection changed.
        /// </summary>
        /// <param name="sender">
        /// The sender. 
        /// </param>
        /// <param name="e">
        /// The e. 
        /// </param>
        private void messagesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If there are no selected items, then disable the Delete button, clear the boxes, and return
            if (e.AddedItems.Count == 0)
            {
                this.deleteButton.IsEnabled = false;
                this.forwardButton.IsEnabled = false;
                this.rawView.Text = string.Empty;
                this.bodyView.Text = string.Empty;
                this.htmlViewTab.Visibility = Visibility.Hidden;
                this.tabControl.SelectedIndex = this.defaultTab.IsVisible ? 0 : 1;

                return;
            }

            var setTitle = new Action<string>(t =>
                {
                    this.Subject.Content = t;
                    this.Subject.ToolTip = t;
                });

            var mailFile = ((MessageEntry)e.AddedItems[0]).File;

            try
            {
                this.tabControl.IsEnabled = false;
                this.SpinAnimation.Visibility = Visibility.Visible;
                setTitle("Loading...");

                if (this._currentMessageCancellationTokenSource != null)
                {
                    this._currentMessageCancellationTokenSource.Cancel();
                }

                this._currentMessageCancellationTokenSource = new CancellationTokenSource();

                Task.Factory.StartNew(() => { })
                    .ContinueWith(task => File.ReadAllLines(mailFile, Encoding.ASCII),
                                  this._currentMessageCancellationTokenSource.Token,
                                  TaskContinuationOptions.NotOnCanceled,
                                  TaskScheduler.Default)
                    .ContinueWith(task =>
                        {
                            // Load the MIME body
                            var mimeReader = new MimeReader(task.Result);
                            MimeEntity me = mimeReader.CreateMimeEntity();

                            return Tuple.Create(task.Result, me.ToMailMessageEx());
                        },
                                  this._currentMessageCancellationTokenSource.Token,
                                  TaskContinuationOptions.NotOnCanceled,
                                  TaskScheduler.Default).ContinueWith(task =>
                                      {
                                          var resultTuple = task.Result;
                                          var mailMessageEx = resultTuple.Item2;

                                          // set the raw view...
                                          this.rawView.Text = string.Join("\n", resultTuple.Item1);

                                          this.bodyView.Text = mailMessageEx.Body;
                                          this.bodyViewTab.Visibility = Visibility.Visible;

                                          this.defaultBodyView.Text = mailMessageEx.Body;

                                          this.FromEdit.Text = mailMessageEx.From.ToString();
                                          this.ToEdit.Text = mailMessageEx.To.ToString();
                                          this.DateEdit.Text = mailMessageEx.DeliveryDate.ToString();
                                          this.SubjectEdit.Text = mailMessageEx.Subject;

                                          setTitle(mailMessageEx.Subject);

                                          // If it is HTML, render it to the HTML view
                                          if (mailMessageEx.IsBodyHtml)
                                          {
                                              if (task.IsCanceled)
                                              {
                                                  return;
                                              }

                                              this.SetBrowserDocument(mailMessageEx);
                                              this.htmlViewTab.Visibility = Visibility.Visible;

                                              this.defaultHtmlView.Visibility = Visibility.Visible;
                                              this.defaultBodyView.Visibility = Visibility.Collapsed;
                                          }
                                          else
                                          {
                                              this.htmlViewTab.Visibility = Visibility.Hidden;
                                              if (this.defaultTab.IsVisible)
                                              {
                                                  this.tabControl.SelectedIndex = 0;
                                              }
                                              else if (Equals(this.tabControl.SelectedItem, this.htmlViewTab))
                                              {
                                                  this.tabControl.SelectedIndex = 2;
                                              }

                                              this.defaultHtmlView.Visibility = Visibility.Collapsed;
                                              this.defaultBodyView.Visibility = Visibility.Visible;
                                          }

                                          this.SpinAnimation.Visibility = Visibility.Collapsed;
                                          this.tabControl.IsEnabled = true;

                                          // Enable the delete and forward button
                                          this.deleteButton.IsEnabled = true;
                                          this.forwardButton.IsEnabled = true;
                                      },
                                                                      this._currentMessageCancellationTokenSource.Token,
                                                                      TaskContinuationOptions.NotOnCanceled,
                                                                      TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception ex)
            {
                Logger.WriteWarning(string.Format(@"Unable to Load Message ""{0}"": {1}", mailFile, ex));

                setTitle("Papercut");
                this.tabControl.SelectedIndex = 1;
                this.bodyViewTab.Visibility = Visibility.Hidden;
                this.htmlViewTab.Visibility = Visibility.Hidden;
            }
        }

        #endregion
    }
}