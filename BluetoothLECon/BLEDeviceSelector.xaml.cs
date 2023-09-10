// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BluetoothLECon
{
    using Microsoft.UI.Xaml.Controls;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using Windows.Devices.Enumeration;

    public sealed partial class BLEDeviceSelector : UserControl
    {
        private ObservableCollection<DeviceInformation> KnownDevices = new ObservableCollection<DeviceInformation>();
        private DeviceWatcher deviceWatcher;

        public delegate void OnSelectEventHandler(object sender, DeviceInformation device);
        public event OnSelectEventHandler OnSelect;

        public BLEDeviceSelector()
        {
            this.InitializeComponent();
            this.StartBleDeviceWatcher();
        }

        private void SelectedDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = this.SelectedDevice.SelectedItem as ComboBoxItem;
            if (item == null) return;

            OnSelect?.Invoke(this, this.KnownDevices.Single(x => x.Name == item.Content.ToString()));
        }

        private void RefreshList()
        {
            var curr = (ComboBoxItem)this.SelectedDevice.SelectedItem;
            this.SelectedDevice.Items.Clear();
            foreach (var device in this.KnownDevices)
            {
                this.SelectedDevice.Items.Add(new ComboBoxItem { Content = device.Name });
            }

            if (curr != null)
            {
                var refreshedCurr = this.SelectedDevice.Items.Cast<ComboBoxItem>().SingleOrDefault(x => (string) x.Content == (string) curr.Content);
                if (refreshedCurr != null)
                {
                    this.SelectedDevice.SelectedItem = refreshedCurr;
                }
            }
        }

        private void StartBleDeviceWatcher()
        {
            // Additional properties we would like about the device.
            // Property strings are documented here https://msdn.microsoft.com/en-us/library/windows/desktop/ff521659(v=vs.85).aspx
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected", "System.Devices.Aep.Bluetooth.Le.IsConnectable" };

            // BT_Code: Example showing paired and non-paired in a single query.
            string aqsAllBluetoothLEDevices = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";

            this.deviceWatcher =
                    DeviceInformation.CreateWatcher(
                        aqsAllBluetoothLEDevices,
                        requestedProperties,
                        DeviceInformationKind.AssociationEndpoint);

            // Register event handlers before starting the watcher.
            this.deviceWatcher.Added += this.DeviceWatcher_Added;
            this.deviceWatcher.Updated += this.DeviceWatcher_Updated;

            // Start over with an empty collection.
            this.KnownDevices.Clear();

            // Start the watcher. Active enumeration is limited to approximately 30 seconds.
            // This limits power usage and reduces interference with other Bluetooth activities.
            // To monitor for the presence of Bluetooth LE devices for an extended period,
            // use the BluetoothLEAdvertisementWatcher runtime class. See the BluetoothAdvertisement
            // sample for an example.
            this.deviceWatcher.Start();
        }

        private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (!this.KnownDevices.Any(x => x.Id == deviceInfo.Id))
                {
                    if (deviceInfo.Name != string.Empty)
                    {
                        this.KnownDevices.Add(deviceInfo);
                        this.RefreshList();
                    }
                }
            });

        }

        private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                var bleDeviceDisplay = this.KnownDevices.SingleOrDefault(x => x.Id == deviceInfoUpdate.Id);
                if (bleDeviceDisplay != null)
                {
                    bleDeviceDisplay.Update(deviceInfoUpdate);
                    this.RefreshList();
                }
            });
        }
    }
}
