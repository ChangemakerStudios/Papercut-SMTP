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
    using System.Text.RegularExpressions;
    using System.Windows;

    using Papercut.Helpers;
    using Papercut.Properties;

    public partial class OptionsWindow : Window
    {
        static readonly Regex _ipv4 = new Regex(
            @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$",
            RegexOptions.Compiled);

        public OptionsWindow()
        {
            InitializeComponent();

            // Add the Any option
            ipsList.Items.Add("Any");

            // Add local IPs
            foreach (IPAddress address in
                Dns.GetHostAddresses("localhost")
                    .Where(address => _ipv4.IsMatch(address.ToString())))
            {
                ipsList.Items.Add(address.ToString());
            }

            // Get NIC IPs
            foreach (string address in
                NetworkHelper.GetIPAddresses().Where(address => _ipv4.IsMatch(address)))
            {
                ipsList.Items.Add(address);
            }

            // Select the current one
            ipsList.SelectedItem = Settings.Default.IP;

            // Set the other options
            portNumber.Text = Settings.Default.Port.ToString();
            startMinimized.IsChecked = Settings.Default.StartMinimized;
            minimizeOnClose.IsChecked = Settings.Default.MinimizeOnClose;
        }

        void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        void saveButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.IP = (string)ipsList.SelectedValue;
            Settings.Default.Port = int.Parse(portNumber.Text);

            if (startMinimized.IsChecked.HasValue) Settings.Default.StartMinimized = startMinimized.IsChecked.Value;
            if (minimizeOnClose.IsChecked.HasValue) Settings.Default.MinimizeOnClose = minimizeOnClose.IsChecked.Value;

            Settings.Default.Save();

            DialogResult = true;
        }
    }
}