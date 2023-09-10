using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography;

public static class GattDeviceServiceExtensions
{
    public static async Task<GattWriteResult> WriteStringAsync(this GattCharacteristic characteristic, string value)
    {
        var buf = CryptographicBuffer.ConvertStringToBinary(value, BinaryStringEncoding.Utf8);

        return await characteristic.WriteValueWithResultAsync(buf);
    }
}