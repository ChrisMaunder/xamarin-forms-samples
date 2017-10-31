using System;
using System.Collections.Generic;
using Android.Bluetooth;
using System.Threading.Tasks;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
    /// <summary>
    /// TODO: this really should be a singleton.
    /// </summary>
    public class Adapter : Java.Lang.Object, BluetoothAdapter.ILeScanCallback, IAdapter
    {
        // events
        public event EventHandler<DeviceDiscoveredEventArgs> DeviceDiscovered   = delegate { };
        public event EventHandler<DeviceConnectionEventArgs> DeviceConnected    = delegate { };
        public event EventHandler<DeviceConnectionEventArgs> DeviceDisconnected = delegate { };
        public event EventHandler ScanTimeoutElapsed = delegate { };

        // class members
        protected BluetoothManager _manager;
        protected BluetoothAdapter _adapter;
        protected GattCallback _gattCallback;

        /// <summary>
        /// Whether or not we're currently scanning for peripheral devices.
        /// </summary>
        public bool IsScanning { get; private set; }

        /// <summary>
        /// Gets the discovered peripherals.
        /// </summary>
        public IList<IDevice> DiscoveredDevices { get; private set; } = new List<IDevice>();

        /// <summary>
        /// Gets the connected peripherals.
        /// </summary>
        public IList<IDevice> ConnectedDevices { get; private set; } = new List<IDevice>();

        public Adapter()
        {
            var appContext = Android.App.Application.Context;

            // get a reference to the bluetooth system service
            _manager = (BluetoothManager)appContext.GetSystemService("bluetooth");
            _adapter = _manager.Adapter;

            _gattCallback = new GattCallback(this);

            _gattCallback.DeviceConnected += (object sender, DeviceConnectionEventArgs e) =>
            {
                ConnectedDevices.Add(e.Device);
                DeviceConnected(this, e);
            };

            _gattCallback.DeviceDisconnected += (object sender, DeviceConnectionEventArgs e) =>
            {
                // TODO: remove the disconnected device from the _connectedDevices list
                // i don't think this will actually work, because i'm created a new underlying device here.
                //if(_connectedDevices.Contains(
                DeviceDisconnected(this, e);
            };
        }

        //TODO: scan for specific service type eg. HeartRateMonitor
        public async void StartScanningForDevices(Guid serviceUuid)
        {
            StartScanningForDevices();
        }

        public async void StartScanningForDevices()
        {
            Console.WriteLine("Adapter: Starting a scan for devices.");

            // clear out the list
            DiscoveredDevices = new List<IDevice>();

            // start scanning
            IsScanning = true;

            /*
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                // The documentation both on the Xamarin and Android sides is non-existent.

                ScanSettings.Builder builder = new ScanSettings.Builder();
                builder.SetScanMode(Android.Bluetooth.LE.ScanMode.LowLatency);
                ScanSettings settings = builder.Build();

                var filters = new ArrayList<ScanFilter>();

                _adapter.StartScan(filters, settings, OnLeScan);
            }
            else
            */
            _adapter.StartLeScan(this);

            // in 10 seconds, stop the scan
            await Task.Delay(10000);

            // if we're still scanning
            if (IsScanning)
            {
                Console.WriteLine("Adapter: Scan timeout has elapsed.");
                _adapter.StopLeScan(this);
                ScanTimeoutElapsed(this, new EventArgs());
            }
        }

        public void StopScanningForDevices()
        {
            Console.WriteLine("Adapter: Stopping the scan for devices.");
            IsScanning = false;
            _adapter.StopLeScan(this);
        }

        public void OnLeScan(BluetoothDevice bleDevice, int rssi, byte[] scanRecord)
        {
            Console.WriteLine("Adapter.LeScanCallback: " + bleDevice.Name);
            // TODO: for some reason, this doesn't work, even though they have the same pointer,
            // it thinks that the item doesn't exist. so i had to write my own implementation
            //			if(!_discoveredDevices.Contains(device) ) {
            //				_discoveredDevices.Add (device );
            //			}

            Device device = new Device(bleDevice, null, null, rssi);

            if (!DeviceExistsInDiscoveredList(bleDevice))
            {
                DiscoveredDevices.Add(device);

                // TODO: in the cross platform API, cache the RSSI
                // TODO: shouldn't i only raise this if it's not already in the list?
                DeviceDiscovered(this, new DeviceDiscoveredEventArgs { Device = device });
            }
        }

        protected bool DeviceExistsInDiscoveredList(BluetoothDevice device)
        {
            foreach (var discoveredDevice in DiscoveredDevices)
            {
                // TODO: verify that address is unique
                if (device.Address == ((BluetoothDevice)discoveredDevice.NativeDevice).Address)
                    return true;
            }

            return false;
        }

        public void ConnectToDevice(IDevice device)
        {
            // returns the BluetoothGatt, which is the API for BLE stuff
            // TERRIBLE API design on the part of google here.
            ((BluetoothDevice)device.NativeDevice).ConnectGatt(Android.App.Application.Context, 
                                                               true, _gattCallback);
        }

        public void DisconnectDevice(IDevice device)
        {
            ((Device)device).Disconnect();
        }
    }
}

