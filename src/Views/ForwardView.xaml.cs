/*  
 * Papercut
 *
 *  Copyright © 2008 - 2012 Ken Robertson
 *  Copyright © 2013 - 2014 Jaben Cargman
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
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows;

    using Papercut.Core.Message;
    using Papercut.Properties;
    using Papercut.Service;

    /// <summary>
    ///     Interaction logic for ForwardWindow.xaml
    /// </summary>
    public partial class ForwardWindow : Window
    {
        static readonly Regex _emailRegex =
            new Regex(
                @"(\A(\s*)\Z)|(\A([^@\s]+)@((?:[-a-z0-9]+\.)+[a-z]{2,})\Z)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        Task _worker;

        bool _working;

        public ForwardWindow(MessageRepository messageRepository)
        {
            MessageRepository = messageRepository;

            InitializeComponent();

            // Load previous settings
            server.Text = Settings.Default.ForwardServer;
            to.Text = Settings.Default.ForwardTo;
            @from.Text = Settings.Default.ForwardFrom;

            server.Focus();
        }

        public MessageEntry MessageEntry { get; set; }

        public MessageRepository MessageRepository { get; set; }

        void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_working)
            {
                _worker.Dispose();
                sendingLabel.Visibility = Visibility.Hidden;
            }

            DialogResult = false;
        }

        void sendButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(server.Text) || string.IsNullOrEmpty(this.@from.Text)
                || string.IsNullOrEmpty(this.to.Text))
            {
                MessageBox.Show(
                    "All the text boxes are required, fill them in please.",
                    "Papercut",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!_emailRegex.IsMatch(this.@from.Text) || !_emailRegex.IsMatch(this.to.Text))
            {
                MessageBox.Show(
                    "You need to enter valid email addresses.",
                    "Papercut",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string host = server.Text.Trim();
            string from = this.@from.Text.Trim();
            string to = this.to.Text.Trim();

            _worker = Task.Factory.StartNew(
                () =>
                {
                    var session = new SmtpSession { MailFrom = from, Sender = host };
                    session.Recipients.Add(to);
                    session.Message = MessageRepository.GetMessage(MessageEntry);

                    new SmtpClient(session).Send();
                });

            _worker.ContinueWith(
                t =>
                {
                    // Save settings for the next time
                    Settings.Default.ForwardServer = server.Text;
                    Settings.Default.ForwardTo = this.to.Text;
                    Settings.Default.ForwardFrom = this.@from.Text;
                    Settings.Default.Save();

                    _working = false;
                    sendingLabel.Visibility = Visibility.Hidden;
                    DialogResult = true;
                },
                TaskScheduler.FromCurrentSynchronizationContext());

            _working = true;
            sendButton.IsEnabled = false;
            sendingLabel.Visibility = Visibility.Visible;
        }
    }
}