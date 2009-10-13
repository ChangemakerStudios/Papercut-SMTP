using System.Collections.Generic;
using System.Management;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using Papercut.Properties;

namespace Papercut
{
	/// <summary>
	/// Interaction logic for OptionsWindow.xaml
	/// </summary>
	public partial class OptionsWindow : Window
	{
		static readonly Regex ipv4 = new Regex(@"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$", RegexOptions.Compiled);

		public OptionsWindow()
		{
			InitializeComponent();

			// Add the Any option
			ipsList.Items.Add("Any");

			// Add local IPs
			foreach (IPAddress address in Dns.GetHostAddresses("localhost"))
				if (ipv4.IsMatch(address.ToString()))
					ipsList.Items.Add(address.ToString());

			// Get NIC IPs
			foreach (string address in GetIpAddresses())
				if (ipv4.IsMatch(address))
					ipsList.Items.Add(address);

			// Select the current one
			ipsList.SelectedItem = Settings.Default.IP;

			// Set the other options
			portNumber.Text = Settings.Default.Port.ToString();
			startMinimized.IsChecked = Settings.Default.StartMinimized;
		}

		private void saveButton_Click(object sender, RoutedEventArgs e)
		{
			Settings.Default.IP = (string)ipsList.SelectedValue;
			Settings.Default.Port = int.Parse(portNumber.Text);
			if(startMinimized.IsChecked.HasValue)
				Settings.Default.StartMinimized = startMinimized.IsChecked.Value;
			Settings.Default.Save();
			DialogResult = true;
		}

		private void cancelButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
		}

		List<string> GetIpAddresses()
		{
			List<string> ips = new List<string>();

			string query = "SELECT IPAddress from Win32_NetworkAdapterConfiguration WHERE IPEnabled=true";
			ManagementObjectCollection mgtObjects = new ManagementObjectSearcher(query).Get();

			foreach (ManagementObject mo in mgtObjects)
			{
				PropertyData ipaddr = mo.Properties["IPAddress"];
				if (ipaddr.IsLocal)
				{
					if (ipaddr.IsArray)
					{
						foreach (string ip in (string[])ipaddr.Value)
							ips.Add(ip);
					}
					else
						ips.Add(ipaddr.Value.ToString());
				}
			}

			return ips;
		}
	}
}