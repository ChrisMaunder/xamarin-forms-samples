using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using CoreBluetooth;
using CoreFoundation;

namespace Robotics.Mobile.Core.iOS
{
    /// <summary>
    /// Manager class for Bluetooth Low Energy connectivity. Adds functionality to the 
    /// CoreBluetooth Manager to track discovered devices, scanning state, and automatically
    /// stops scanning after a timeout period.
    /// </summary>
    public class BluetoothLEManager
    {
        // event declarations
        public event EventHandler<CBDiscoveredPeripheralEventArgs> DeviceDiscovered   = delegate { };
        public event EventHandler<CBPeripheralEventArgs>           DeviceConnected    = delegate { };
        public event EventHandler<CBPeripheralErrorEventArgs>      DeviceDisconnected = delegate { };
        public event EventHandler                                  ScanTimeoutElapsed = delegate { };

        protected const int _scanTimeout = 10000;

        /// <summary>
        /// Whether or not we're currently scanning for peripheral devices.
        /// </summary>
        public bool IsScanning { get; protected set; }

        /// <summary>
        /// Gets the discovered peripherals.
        /// </summary>
        public List<CBPeripheral> DiscoveredDevices { get; private set; } = new List<CBPeripheral>();

        /// <summary>
        /// Gets the connected peripherals.
        /// </summary>
        public IList<CBPeripheral> ConnectedDevices { get; private set; } = new List<CBPeripheral>();

        /// <summary>
        /// The central BLE manager.
        /// </summary>
        public CBCentralManager CentralManager { get; private set; }

        /// <summary>
        /// The current central BLE manager.
        /// </summary>
        public static BluetoothLEManager Current { get; private set; }

        static BluetoothLEManager()
        {
            Current = new BluetoothLEManager();
        }

        protected BluetoothLEManager()
        {
            CentralManager = new CBCentralManager(DispatchQueue.CurrentQueue);
            CentralManager.DiscoveredPeripheral += (object sender, CBDiscoveredPeripheralEventArgs e) =>
            {
                Console.WriteLine("DiscoveredPeripheral: " + e.Peripheral.Name);
                DiscoveredDevices.Add(e.Peripheral);
                DeviceDiscovered(this, e);
            };

            CentralManager.UpdatedState += (object sender, EventArgs e) =>
            {
                Console.WriteLine("UpdatedState: " + CentralManager.State);
            };


            CentralManager.ConnectedPeripheral += (object sender, CBPeripheralEventArgs e) =>
            {
                Console.WriteLine("ConnectedPeripheral: " + e.Peripheral.Name);

                // When a peripheral gets connected, add that peripheral to our running list of 
                // connected peripherals
                if (!ConnectedDevices.Contains(e.Peripheral))
                    ConnectedDevices.Add(e.Peripheral);

                // raise our connected event
                DeviceConnected(sender, e);
            };

            CentralManager.DisconnectedPeripheral += (object sender, CBPeripheralErrorEventArgs e) =>
            {
                Console.WriteLine("DisconnectedPeripheral: " + e.Peripheral.Name);

                // When a peripheral disconnects, remove it from our running list.
                if (ConnectedDevices.Contains(e.Peripheral))
                    ConnectedDevices.Remove(e.Peripheral);

                // Raise our disconnected event
                DeviceDisconnected(sender, e);
            };
        }

        /// <summary>
        /// Begins the scanning for bluetooth LE devices. Automatically called after 10 seconds
        /// to prevent battery drain.
        /// </summary>
        /// <returns>The scanning for devices.</returns>
        public async Task BeginScanningForDevices()
        {
            Console.WriteLine("BluetoothLEManager: Starting a scan for devices.");

            // clear out the list
            DiscoveredDevices = new List<CBPeripheral>();

            // start scanning
            IsScanning = true;
            CentralManager.ScanForPeripherals(peripheralUuids: null);

            // in 10 seconds, stop the scan
            await Task.Delay(10000);

            // if we're still scanning
            if (IsScanning)
            {
                Console.WriteLine("BluetoothLEManager: Scan timeout has elapsed.");
                CentralManager.StopScan();
                ScanTimeoutElapsed(this, new EventArgs());
            }
        }

        /// <summary>
        /// Stops the Central Bluetooth Manager from scanning for more devices. Automatically
        /// called after 10 seconds to prevent battery drain. 
        /// </summary>
        public void StopScanningForDevices()
        {
            Console.WriteLine("BluetoothLEManager: Stopping the scan for devices.");
            IsScanning = false;
            CentralManager.StopScan();
        }

        public void DisconnectDevice(CBPeripheral peripheral)
        {
            CentralManager.CancelPeripheralConnection(peripheral);
        }
    }
}

