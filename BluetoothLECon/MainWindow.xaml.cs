namespace BluetoothLECon;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

public class Observable<T> : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    public T Value
    {
        get => this._value; set
        {
            this._value = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Value)));
        }
    }
    private T _value;

    public Observable(T value)
    {
        this.Value = value;
    }

    public Observable() { }
}

public sealed partial class MainWindow : Window
{
    public GattDeviceService? SelectedService;
    public Observable<GattCharacteristic> SelectedChar = new();
    public ObservableCollection<string> Logs = new();

    public MainWindow()
    {
        this.InitializeComponent();

        this.ExtendsContentIntoTitleBar = true;
        this.SetTitleBar(this.AppTitleBar);

        this.SelectedDevice.OnSelect += (sender, device) => this.Connect(device);

        this.SelectedChar.PropertyChanged += this.OnSelectedCharChanged;

        this.Nav.SelectionChanged += (_, _) => {
            var i  = this.Nav.SelectedItem as NavigationViewItem;
            this.KeyboardView.Visibility = i == this.KeyboardMenuItem ? Visibility.Visible : Visibility.Collapsed;
            this.JoystickView.Visibility = i == this.JoystickMenuItem ? Visibility.Visible : Visibility.Collapsed;
        };
    }

    private void OnSelectedCharChanged(object sender, PropertyChangedEventArgs args)
    {
        this.KeyboardView.SelectedChar.Value = this.SelectedChar.Value;
    }

    private void SetAvailableServices(IEnumerable<GattDeviceService> services)
    {
        var curr = (ComboBoxItem)this.SelectedServiceCombo.SelectedItem;

        this.SelectedServiceCombo.Items.Clear();
        foreach (var s in services)
        {
            this.SelectedServiceCombo.Items.Add(new ComboBoxItem { Content = BLEService.GetServiceName(s), Tag = s });
        }

        if (curr != null)
        {
            this.SelectedServiceCombo.SelectedItem = this.SelectedServiceCombo.Items.Cast<ComboBoxItem>().FirstOrDefault(x => (string)x.Content == (string)curr.Content);
        }

        this.SelectedServiceCombo.Opacity = this.SelectedServiceCombo.Items.Count > 0 ? 1 : 0;
    }

    private void SetAvailableChars(IEnumerable<GattCharacteristic> services)
    {
        var curr = (ComboBoxItem)this.SelectedCharCombo.SelectedItem;

        this.SelectedCharCombo.Items.Clear();
        foreach (var s in services)
        {
            this.SelectedCharCombo.Items.Add(new ComboBoxItem { Content = BLEService.GetCharacteristicName(s), Tag = s });
        }

        if (curr != null)
        {
            this.SelectedCharCombo.SelectedItem = this.SelectedCharCombo.Items.Cast<ComboBoxItem>().FirstOrDefault(x => (string)x.Content == (string)curr.Content);
        }

        this.SelectedCharCombo.Opacity = this.SelectedCharCombo.Items.Count > 0 ? 1 : 0;
    }

    public async void Connect(DeviceInformation device)
    {
        var de = await BluetoothLEDevice.FromIdAsync(device.Id);
        if (!de.DeviceInformation.Pairing.IsPaired)
        {
            await de.DeviceInformation.Pairing.PairAsync();
        }
        if (de == null)
        {
            this.Alert("Error", $"Could not connect to {device.Name}");
        }
        de.ConnectionStatusChanged += (sender, args) =>
        {
            if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
            {
                this.WriteLog($"# disconnected from {de.Name}");
            }
        };
        this.WriteLog($"# connected to {de.Name}");
        var servicesReq = await de.GetGattServicesAsync();
        var services = servicesReq.Services.ToList();
        this.SetAvailableServices(services);
        var keyService = services.SingleOrDefault(x => BLEService.GetServiceNativeType(x) == BLEService.GattNativeServiceUuid.SimpleKeyService);
        if (keyService == null)
        {
            this.Alert("Error", $"Unable to access {nameof(BLEService.GattNativeServiceUuid.SimpleKeyService)} on device");
            return;
        }
    }

    private void WriteLog(string msg)
    {
        this.Logs.Add(msg);
    }

    private void SelectedServiceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var item = (ComboBoxItem)this.SelectedServiceCombo.SelectedItem;
        var service = item?.Tag as GattDeviceService;
        this.SelectedService = service;

        if (service != null)
        {
            var chars = service.GetAllCharacteristics();
            if (chars.Count == 0)
            {
                this.Alert("Warning", $"Could not find any characteristic for service {BLEService.GetServiceName(service)}. Try to unpair/pair {this.SelectedDevice.Name}.");
            }

            this.SetAvailableChars(chars);
        }
    }

    private void SelectedCharCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var item = (ComboBoxItem)this.SelectedCharCombo.SelectedItem;
        var characteristic = item?.Tag as GattCharacteristic;
        this.SelectedChar.Value = characteristic;
    }
}
