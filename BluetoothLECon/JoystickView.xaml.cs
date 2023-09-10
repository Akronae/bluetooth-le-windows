namespace BluetoothLECon;

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ComponentModel;

public sealed partial class JoystickView : UserControl
{
    public Observable<GattCharacteristic> SelectedChar { get; set; } = new();
    public ObservableCollection<string> Logs { get; set; } = new ObservableCollection<string> { };

    public JoystickView()
    {
        this.InitializeComponent();
    }

    private async Task<bool> WriteCharacteristicAsync(GattCharacteristic characteristic, string value)
    {
        try
        {
            var result = await characteristic.WriteStringAsync(value);

            if (result.Status == GattCommunicationStatus.Success)
            {
                return true;
            }
            else
            {
                this.Alert("Error", $"Write failed: {result.Status}");
                return false;
            }
        }
        catch (Exception ex)
        {
            this.Alert("Error", $"Write failed: {ex.HResult}");
            return false;
        }
    }

    private void WriteTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Up)
        {
            this.WriteCharacteristicAsync(this.SelectedChar.Value, "0");
        }
        if (e.Key == Windows.System.VirtualKey.Down)
        {
            this.WriteCharacteristicAsync(this.SelectedChar.Value, "180");
        }
        if (e.Key == Windows.System.VirtualKey.Left)
        {
            this.WriteCharacteristicAsync(this.SelectedChar.Value, "90");
        }
        if (e.Key == Windows.System.VirtualKey.Right)
        {
            this.WriteCharacteristicAsync(this.SelectedChar.Value, "270");
        }
    }
}
