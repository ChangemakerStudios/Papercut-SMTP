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
    using System.Linq;
    using System.Net;
    using System.Windows;

    using Papercut.Helpers;
    using Papercut.Properties;

    public partial class OptionsWindow : Window
    {
        #region Constructors and Destructors

        public OptionsWindow()
        {
            this.InitializeComponent();

            // Add the Any option
            this.ipsList.Items.Add("Any");

            // Add local IPs
            foreach (IPAddress address in Dns.GetHostAddresses("localhost").Where(address => NetworkHelper.IsValidIP(address.ToString())))
            {
                this.ipsList.Items.Add(address.ToString());
            }

            // Get NIC IPs
            foreach (string address in NetworkHelper.GetIPAddresses().Where(NetworkHelper.IsValidIP))
            {
                this.ipsList.Items.Add(address);
            }

            // Select the current one
            this.ipsList.SelectedItem = Settings.Default.IP;

            // Set the other options
            this.portNumber.Text = Settings.Default.Port.ToString();
            this.startMinimized.IsChecked = Settings.Default.StartMinimized;
            this.minimizeOnClose.IsChecked = Settings.Default.MinimizeOnClose;
        }

        #endregion

        #region Methods

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.IP = (string)this.ipsList.SelectedValue;
            Settings.Default.Port = int.Parse(this.portNumber.Text);

            if (this.startMinimized.IsChecked.HasValue)
            {
                Settings.Default.StartMinimized = this.startMinimized.IsChecked.Value;
            }
            if (this.minimizeOnClose.IsChecked.HasValue)
            {
                Settings.Default.MinimizeOnClose = this.minimizeOnClose.IsChecked.Value;
            }

            Settings.Default.Save();

            this.DialogResult = true;
        }

        #endregion
    }
}