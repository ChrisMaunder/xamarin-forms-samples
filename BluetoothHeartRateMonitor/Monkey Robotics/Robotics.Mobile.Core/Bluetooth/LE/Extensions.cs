using System;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
    public static class Extensions
    {
        private const string BluetoothGuidPrefix = "0000";
        private const string BluetoothGuidSuffix = "-0000-1000-8000-00805f9b34fb";

        /// <summary>
        /// Create a full Guid from the Bluetooth "Assigned Number" (short version)
        /// </summary>
        /// <returns>a Guid of the form {00002A37-0000-1000-8000-00805f9b34fb}</returns>
        /// <param name="partial">4 digit hex value in string form, eg "2A37" (which is heart rate 
        /// measurement)</param>
        public static Guid GuidFromAssignedNumber(this string partial)
        {
            // We may sometimes only get the significant bits, e.g. 180d or whatever, so we need 
            // to pad the string with 0's
            string id = partial.PadRight(4, '0');

            if (id.Length == 4)
                id = BluetoothGuidPrefix + id + BluetoothGuidSuffix;

            return Guid.ParseExact(id, "d");
        }

        /// <summary>
        /// Create a full Guid from the Bluetooth "Assigned Number" (short version)
        /// </summary>
        /// <returns>a Guid of the form {00002A37-0000-1000-8000-00805f9b34fb}</returns>
        /// <param name="partial">4 digit hex value, eg 0x2A37 (which is heart rate measurement)</param>
        public static Guid GuidFromAssignedNumber(this Int32 partial)
        {
            // We may sometimes only get the significant bits, e.g. 180d or whatever, so we need 
            // to pad the string with 0's
            string id = partial.ToString("X").PadRight(4, '0');

            if (id.Length == 4)
                id = BluetoothGuidPrefix + id + BluetoothGuidSuffix;

            return Guid.ParseExact(id, "d");
        }

        /// <summary>
        /// Extract the Bluetooth "Assigned Number" from a Uuid 
        /// </summary>
        /// <returns>4 digit hex value, eg 0x2A37 (which is heart rate measurement)</returns>
        /// <param name="uuid">a Guid of the form {00002A37-0000-1000-8000-00805f9b34fb}</param>
        public static string AssignedNumberFromGuid(this Guid uuid)
        {
            // opposite of the UuidFromPartial method
            string id = uuid.ToString();
            if (id.Length > BluetoothGuidPrefix.Length + 4)
                id = id.Substring(BluetoothGuidPrefix.Length, 4);

            return "0x" + id;
        }

        /// <summary>
        /// Asynchronously gets the requested service
        /// </summary>
        public static Task<IDevice> ConnectAsync(this IAdapter adapter, IDevice device)
        {
            if (device.State == DeviceState.Connected)
                return Task.FromResult<IDevice>(null);

            var completionSource = new TaskCompletionSource<IDevice>();

            EventHandler<DeviceConnectionEventArgs> handler = null;
            handler = (sender, e) =>
            {
                Debug.WriteLine("CCC: " + e.Device.ID + " " + e.Device.State);
                if (e.Device.ID == device.ID)
                {
                    adapter.DeviceConnected -= handler;
                    completionSource.SetResult(e.Device);
                }
            };

            adapter.DeviceConnected += handler;

            adapter.ConnectToDevice(device);

            return completionSource.Task;
        }

        /// <summary>
        /// Asynchronously gets the requested service
        /// </summary>
        public static Task<IService> GetServiceAsync(this IDevice device, Guid id)
        {
            if (device.Services.Count > 0)
            {
                return Task.FromResult(device.Services.First(x => x.ID == id));
            }

            var completionSource = new TaskCompletionSource<IService>();

            EventHandler handler = null;
            handler = (sender, e) =>
            {
                device.ServicesDiscovered -= handler;
                try
                {
                    var s = device.Services.First(x => x.ID == id);
                    completionSource.SetResult(s);
                }
                catch (Exception ex)
                {
                    completionSource.SetException(ex);
                }
            };

            device.ServicesDiscovered += handler;
            device.DiscoverServices();

            return completionSource.Task;
        }

        /// <summary>
        /// Asynchronously gets the requested characteristic
        /// </summary>
        public static Task<ICharacteristic> GetCharacteristicAsync(this IService service, Guid id)
        {
            if (service.Characteristics.Count > 0)
            {
                return Task.FromResult(service.Characteristics.First(x => x.ID == id));
            }

            var tcs = new TaskCompletionSource<ICharacteristic>();
            EventHandler h = null;
            h = (sender, e) =>
            {
                service.CharacteristicsDiscovered -= h;
                try
                {
                    var s = service.Characteristics.First(x => x.ID == id);
                    tcs.SetResult(s);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            };
            service.CharacteristicsDiscovered += h;
            service.DiscoverCharacteristics();

            return tcs.Task;
        }
    }
}

