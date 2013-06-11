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

namespace Papercut
{
	#region Using

	using System.Collections.Generic;
	using System.Linq;
	using System.Management;
	using System.Net;
	using System.Text.RegularExpressions;
	using System.Windows;

	using Papercut.Properties;

	#endregion

	/// <summary>
	/// Interaction logic for OptionsWindow.xaml
	/// </summary>
	public partial class OptionsWindow : Window
	{
		#region Constants and Fields

		/// <summary>
		/// The ipv 4.
		/// </summary>
		private static readonly Regex ipv4 = new Regex(@"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$", RegexOptions.Compiled);

		#endregion

		#region Constructors and Destructors

		/// <summary>
		/// Initializes a new instance of the <see cref="OptionsWindow"/> class.
		/// </summary>
		public OptionsWindow()
		{
			this.InitializeComponent();

			// Add the Any option
			this.ipsList.Items.Add("Any");

			// Add local IPs
			foreach (IPAddress address in Dns.GetHostAddresses("localhost").Where(address => ipv4.IsMatch(address.ToString())))
			{
				this.ipsList.Items.Add(address.ToString());
			}

			// Get NIC IPs
			foreach (string address in this.GetIpAddresses().Where(address => ipv4.IsMatch(address)))
			{
				this.ipsList.Items.Add(address);
			}

			// Select the current one
            this.ipsList.SelectedItem = Settings.Default.IP;

			// Set the other options
			this.portNumber.Text = Settings.Default.Port.ToString();
			this.startMinimized.IsChecked = Settings.Default.StartMinimized;
			this.showDefaultTab.IsChecked = Settings.Default.ShowDefaultTab;
		}

		#endregion

		#region Methods

		/// <summary>
		/// The get ip addresses.
		/// </summary>
		/// <returns>
		/// </returns>
		private List<string> GetIpAddresses()
		{
			var ips = new List<string>();

			string query = "SELECT IPAddress from Win32_NetworkAdapterConfiguration WHERE IPEnabled=true";
			ManagementObjectCollection mgtObjects = new ManagementObjectSearcher(query).Get();

			foreach (
				PropertyData ipaddr in
					mgtObjects.Cast<ManagementObject>().Select(mo => mo.Properties["IPAddress"]).Where(ipaddr => ipaddr.IsLocal))
			{
				if (ipaddr.IsArray)
				{
					ips.AddRange((string[])ipaddr.Value);
				}
				else
				{
					ips.Add(ipaddr.Value.ToString());
				}
			}

			return ips;
		}

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
			this.DialogResult = false;
		}

		/// <summary>
		/// The save button_ click.
		/// </summary>
		/// <param name="sender">
		/// The sender.
		/// </param>
		/// <param name="e">
		/// The e.
		/// </param>
		private void saveButton_Click(object sender, RoutedEventArgs e)
		{
			Settings.Default.IP = (string)this.ipsList.SelectedValue;
			Settings.Default.Port = int.Parse(this.portNumber.Text);
			if (this.startMinimized.IsChecked.HasValue)
			{
				Settings.Default.StartMinimized = this.startMinimized.IsChecked.Value;
			}

			if (this.showDefaultTab.IsChecked.HasValue)
			{
				Settings.Default.ShowDefaultTab = this.showDefaultTab.IsChecked.Value;
			}

			Settings.Default.Save();
			this.DialogResult = true;
		}

		#endregion
	}
}