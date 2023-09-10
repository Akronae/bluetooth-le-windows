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
using Windows.System;

public sealed partial class KeyboardView : UserControl
{
    public static readonly DependencyProperty LogsProperty = DependencyProperty.Register("Logs", typeof(ObservableCollection<string>), typeof(KeyboardView), new PropertyMetadata(new()));

    public Observable<GattCharacteristic> SelectedChar { get; set; } = new();
    public ObservableCollection<string> Logs { get; set; } = new ObservableCollection<string> { };

    private Dictionary<VirtualKey, bool> KeysDown = new();

    public KeyboardView()
    {
        this.InitializeComponent();

        this.SelectedChar.PropertyChanged += this.OnSelectedCharChanged;
    }

    private void OnSelectedCharChanged(object sender, PropertyChangedEventArgs args)
    {
        if (this.SelectedChar.Value != null)
        {
            this.Receive(this.SelectedChar.Value).RunAsyncBackground();
        }
    }

    public async Task Receive(GattCharacteristic characteristic)
    {
        characteristic.ValueChanged += (sender, args) =>
        {
            var str = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, args.CharacteristicValue);
            this.WriteLog($"[received]: {str}");
        };
        await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
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

    private void WriteLog(string log)
    {
        this.Logs.Add(log);
        this.DispatcherQueue.TryEnqueue(() =>
        {
            if (this.TextBoxLog.Text.Length > 10000)
            {
                this.TextBoxLog.Text = string.Empty;
            }
            this.TextBoxLog.Text = string.Join("\n", this.Logs.ToArray());
        });
    }
    public async void SendText()
    {
        this.WriteLog($"[sent]: {this.WriteTextBox.Text}");
        await this.WriteCharacteristicAsync(this.SelectedChar.Value, this.WriteTextBox.Text);
    }

    private void SendTextButton_Click(object sender, RoutedEventArgs e)
    {
        this.SendText();
    }

    private void WriteTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        this.KeysDown[e.Key] = true;

        if (e.Key == VirtualKey.Enter)
        {
            this.SendText();
        }

        var angle = 0f;
        var pressCount = 0f;
        if (this.KeysDown.GetValueOrDefault(VirtualKey.Up))
        {
            angle += 90;
            pressCount += 1;
        }
        if (this.KeysDown.GetValueOrDefault(VirtualKey.Down))
        {
            angle += 270;
            pressCount += 1;
        }
        if (this.KeysDown.GetValueOrDefault(VirtualKey.Left))
        {
            angle += 180;
            pressCount += 1;
        }
        if (this.KeysDown.GetValueOrDefault(VirtualKey.Right))
        {
            angle += 0;
            pressCount += 1;
        }

        if (pressCount > 0)
        {
            var final = angle / pressCount;
            this.WriteCharacteristicAsync(this.SelectedChar.Value, $"move {final}");
            Debug.WriteLine(final);
        }
    }

    private void WriteTextBox_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        this.KeysDown[e.Key] = false;

        if (this.SelectedChar.Value != null)
        {
            this.WriteCharacteristicAsync(this.SelectedChar.Value, "stop");
        }
    }
}
