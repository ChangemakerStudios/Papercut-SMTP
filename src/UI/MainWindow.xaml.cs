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
    using System.Security.AccessControl;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Forms;
    using System.Windows.Input;
    using System.Windows.Navigation;
    using System.Windows.Threading;

    using MimeKit;

    using Papercut.Mime;
    using Papercut.Properties;
    using Papercut.SMTP;

    using Application = System.Windows.Application;
    using ContextMenu = System.Windows.Forms.ContextMenu;
    using DataFormats = System.Windows.DataFormats;
    using DataObject = System.Windows.DataObject;
    using DragDropEffects = System.Windows.DragDropEffects;
    using KeyEventArgs = System.Windows.Input.KeyEventArgs;
    using ListBox = System.Windows.Controls.ListBox;
    using MenuItem = System.Windows.Forms.MenuItem;
    using MessageBox = System.Windows.MessageBox;
    using MouseEventArgs = System.Windows.Input.MouseEventArgs;
    using Point = System.Windows.Point;

    #endregion

    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields

        private readonly object deleteLockObject = new object();

        private readonly NotifyIcon notification;

        private readonly Server server;

        private CancellationTokenSource _currentMessageCancellationTokenSource = null;

        #endregion

        #region Constructors and Destructors

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

            this.notification.BalloonTipClicked += (sender, args) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.messagesList.SelectedIndex = this.messagesList.Items.Count - 1;
            };

            this.notification.ContextMenu = new ContextMenu(
                new[]
                {
                    new MenuItem(
                        "Show",
                        (sender, args) =>
                        {
                            this.Show();
                            this.WindowState = WindowState.Normal;
                            this.Focus();
                        }) { DefaultItem = true },
                    new MenuItem("Exit", (sender, args) => this.ExitApplication())
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

            this.UpdateSelectedMessage();

            // Minimize if set to
            if (Settings.Default.StartMinimized)
            {
                this.Hide();
            }
        }

        #endregion

        #region Delegates

        private delegate void MessageNotificationDelegate(MessageEntry entry);

        #endregion

        #region Methods

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
        ///     Add a newly received message and show the balloon notification
        /// </summary>
        /// <param name="entry">
        ///     The entry.
        /// </param>
        private void AddNewMessage(MessageEntry entry)
        {
            // Add it to the list box
            this.messagesList.Items.Add(entry);

            // Show the notification
            this.notification.ShowBalloonTip(5000, string.Empty, "New message received!", ToolTipIcon.Info);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.ExitApplication(true);
        }

        private void ExitApplication(bool closeWindow = true)
        {
            this.notification.Dispose();
            this.server.Stop();
            if (closeWindow)
            {
                this.Close();
            }
            Environment.Exit(0);
        }

        private void GoToSite(object sender, MouseButtonEventArgs e)
        {
            Process.Start("http://papercut.codeplex.com/");
        }

        /// <summary>
        ///     Load existing messages from the file system
        /// </summary>
        private void LoadMessages()
        {
            foreach (var entry in MessageFileService.LoadMessages())
            {
                this.messagesList.Items.Add(entry);
            }
        }

        /// <summary>
        /// Handles the OnKeyDown event of the MessagesList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="KeyEventArgs"/> instance containing the event data.</param>
        private void MessagesList_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete)
            {
                return;
            }

            this.DeleteSelectedMessage();
        }

        private void DeleteSelectedMessage()
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
                    MessageFileService.DeleteMessage(entry);
                    this.messagesList.Items.Remove(entry);
                }

                this.UpdateSelectedMessage(index);
            }
        }

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
            catch (Exception)
            {
                MessageBox.Show(
                    "Failed to rebind to the address/port specified.  The port may already be in use by another process.  Please update the configuration.",
                    "Operation Failure");
                this.Options_Click(null, null);
            }
        }

        private void Processor_MessageReceived(object sender, MessageEventArgs e)
        {
            // This takes place on a background thread from the SMTP server
            // Dispatch it back to the main thread for the update
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new MessageNotificationDelegate(this.AddNewMessage), e.Entry);
        }

        /// <summary>
        ///     Write the HTML to a temporary file and render it to the HTML view
        /// </summary>
        /// <param name="mailMessageEx">
        ///     The mail Message Ex.
        /// </param>
        private void SetBrowserDocument(MimeMessage mailMessageEx)
        {
            const int Length = 256;

            var replaceEmbeddedImageFormats = new[]
                                           {
                                               @"cid:{0}", @"cid:'{0}'", @"cid:""{0}"""
                                           };

            string tempPath = Path.GetTempPath();
            string htmlFile = Path.Combine(tempPath, "papercut.htm");

            string htmlText = mailMessageEx.BodyParts.GetMainBodyTextPart().Text;

            foreach (var image in mailMessageEx.GetImages().Where(i => !string.IsNullOrWhiteSpace(i.ContentId)))
            {
                string fileName = Path.Combine(tempPath, image.ContentId);

                using (var fs = File.OpenWrite(fileName))
                {
                    var buffer = new byte[Length];

                    using (var content = image.ContentObject.Open())
                    {
                        int bytesRead = content.Read(buffer, 0, Length);

                        // write the required bytes
                        while (bytesRead > 0)
                        {
                            fs.Write(buffer, 0, bytesRead);
                            bytesRead = content.Read(buffer, 0, Length);
                        }
                    }

                    fs.Close();
                }

                htmlText = replaceEmbeddedImageFormats.Aggregate(
                    htmlText,
                    (current, format) => current.Replace(string.Format(format, image.ContentId), image.ContentId));
            }

            File.WriteAllText(htmlFile, htmlText, Encoding.Unicode);

            //this.htmlView.Navigate(new Uri(htmlFile));
            //this.htmlView.Refresh();

            this.defaultHtmlView.NavigationUIVisibility = NavigationUIVisibility.Hidden;
            this.defaultHtmlView.Navigate(new Uri(htmlFile));
            this.defaultHtmlView.Refresh();
        }

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


        private void Window_Closing(object sender, CancelEventArgs e)
        {
            //Cancel close and minimize if setting is set to minimize on close
            if (Settings.Default.MinimizeOnClose)
            {
                e.Cancel = true;
                this.WindowState = WindowState.Minimized;
            }
            else
            {
                this.ExitApplication(false);
            }
        }

        /// <summary>
        /// Handles the Click event of the deleteButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            this.DeleteSelectedMessage();
        }

        private void forwardButton_Click(object sender, RoutedEventArgs e)
        {
            var entry = this.messagesList.SelectedItem as MessageEntry;
            if (entry != null)
            {
                var fw = new ForwardWindow(entry.File) { Owner = this };
                fw.ShowDialog();
            }
        }

        private void SetWindowTitle(string title)
        {
            this.Subject.Content = title;
            this.Subject.ToolTip = title;
        }

        private void messagesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If there are no selected items, then disable the Delete button, clear the boxes, and return
            if (e.AddedItems.Count == 0)
            {
                this.deleteButton.IsEnabled = false;
                this.forwardButton.IsEnabled = false;
                this.headerView.Text = string.Empty;
                this.bodyView.Text = string.Empty;
                this.textViewTab.Visibility = Visibility.Hidden;
                this.tabControl.SelectedIndex = this.defaultTab.IsVisible ? 0 : 1;
               
                // Clear fields
                this.FromEdit.Text = string.Empty;
                this.ToEdit.Text = string.Empty;
                this.CCEdit.Text = string.Empty;
                this.BccEdit.Text = string.Empty;
                this.DateEdit.Text = string.Empty;

                var subject = string.Empty;
                this.SubjectEdit.Text = subject;

                this.defaultBodyView.Text = string.Empty;

                this.defaultHtmlView.Content = null;
                this.defaultHtmlView.NavigationService.RemoveBackEntry();
                //this.defaultHtmlView.Refresh();

                this.SetWindowTitle("Papercut");

                return;
            }

            var mailFile = ((MessageEntry)e.AddedItems[0]).File;

            try
            {
                this.tabControl.IsEnabled = false;
                this.SpinAnimation.Visibility = Visibility.Visible;

                this.SetWindowTitle("Loading...");

                if (this._currentMessageCancellationTokenSource != null)
                {
                    this._currentMessageCancellationTokenSource.Cancel();
                }

                this._currentMessageCancellationTokenSource = new CancellationTokenSource();

                var loadMessageTask = MessageHelper.LoadMessage(mailFile, this._currentMessageCancellationTokenSource.Token);

                // show it...
                loadMessageTask.ContinueWith(
                    task => this.DisplayMimeMessage(task.Result),
                    this._currentMessageCancellationTokenSource.Token,
                    TaskContinuationOptions.NotOnCanceled,
                    TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception ex)
            {
                Logger.WriteWarning(string.Format(@"Unable to Load Message ""{0}"": {1}", mailFile, ex));

                this.SetWindowTitle("Papercut");
                this.tabControl.SelectedIndex = 1;
                this.bodyViewTab.Visibility = Visibility.Hidden;
                this.textViewTab.Visibility = Visibility.Hidden;
            }
        }

        private void DisplayMimeMessage(MimeMessage mailMessageEx)
        {
            this.headerView.Text = string.Join("\r\n", mailMessageEx.Headers.Select(h => h.ToString()));

            var parts = mailMessageEx.BodyParts.ToList();
            var mainBody = parts.GetMainBodyTextPart();

            this.bodyView.Text = mainBody.Text;
            this.bodyViewTab.Visibility = Visibility.Visible;

            this.defaultBodyView.Text = mainBody.Text;

            this.FromEdit.Text = mailMessageEx.From.IfNotNull(s => s.ToString()) ?? string.Empty;
            this.ToEdit.Text = mailMessageEx.To.IfNotNull(s => s.ToString()) ?? string.Empty;
            this.CCEdit.Text = mailMessageEx.Cc.IfNotNull(s => s.ToString()) ?? string.Empty;
            this.BccEdit.Text = mailMessageEx.Bcc.IfNotNull(s => s.ToString()) ?? string.Empty;
            this.DateEdit.Text = mailMessageEx.Date.IfNotNull(s => s.ToString()) ?? string.Empty;

            var subject = mailMessageEx.Subject ?? string.Empty;
            this.SubjectEdit.Text = subject;

            this.SetWindowTitle(subject);

            var isContentHtml = mainBody.IsContentHtml();
            textViewTab.Visibility = Visibility.Hidden;

            if (isContentHtml)
            {
                this.SetBrowserDocument(mailMessageEx);

                var textPartNotHtml = parts.OfType<TextPart>().Except(new[] { mainBody }).FirstOrDefault();
                if (textPartNotHtml != null)
                {
                    textViewTab.Visibility = Visibility.Visible;
                    textView.Text = textPartNotHtml.Text;

                    if (Equals(this.tabControl.SelectedItem, this.textViewTab))
                    {
                        this.tabControl.SelectedIndex = 2;
                    }
                }
            }

            if (this.defaultTab.IsVisible)
            {
                this.tabControl.SelectedIndex = 0;
            }

            this.defaultHtmlView.Visibility = isContentHtml ? Visibility.Visible : Visibility.Collapsed;
            this.defaultBodyView.Visibility = isContentHtml ? Visibility.Collapsed : Visibility.Visible;

            this.SpinAnimation.Visibility = Visibility.Collapsed;
            this.tabControl.IsEnabled = true;

            // Enable the delete and forward button
            this.deleteButton.IsEnabled = true;
            this.forwardButton.IsEnabled = true;
        }

        #endregion
        
        Point? _dragStartPoint = null;

        private void MessagesList_OnPreviewLeftMouseDown(object sender, MouseButtonEventArgs e)
        {
            var parent = sender as ListBox;

            if (parent == null)
            {
                return;
            }

            if (this._dragStartPoint == null)
            {
                this._dragStartPoint = e.GetPosition(parent);
            }
        }

        private void MessagesList_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            this._dragStartPoint = null;
        }

        private void MessagesList_OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            var parent = sender as ListBox;

            if (parent == null || this._dragStartPoint == null)
            {
                return;
            }

            var dragPoint = e.GetPosition(parent);

            Vector potentialDragLength = dragPoint - this._dragStartPoint.Value;

            if (potentialDragLength.Length > 10)
            {
                // Get the object source for the selected item
                var entry = parent.GetObjectDataFromPoint<MessageEntry>(this._dragStartPoint.Value);

                // If the data is not null then start the drag drop operation
                if (!string.IsNullOrWhiteSpace(entry.File))
                {
                    var dataObject = new DataObject(DataFormats.FileDrop, new[] { entry.File });
                    DragDrop.DoDragDrop(parent, dataObject, DragDropEffects.Copy);
                }

                this._dragStartPoint = null;
            }
        }
    }
}