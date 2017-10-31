using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

using CoreBluetooth;
using CoreFoundation;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
    public class Adapter : IAdapter
    {
        readonly AutoResetEvent _stateChanged = new AutoResetEvent(false);

        // events
        public event EventHandler<DeviceDiscoveredEventArgs> DeviceDiscovered      = delegate { };
        public event EventHandler<DeviceConnectionEventArgs> DeviceConnected       = delegate { };
        public event EventHandler<DeviceConnectionEventArgs> DeviceDisconnected    = delegate { };
        public event EventHandler<DeviceConnectionEventArgs> DeviceFailedToConnect = delegate { };
        public event EventHandler                            ScanTimeoutElapsed    = delegate { };
        public event EventHandler                            ConnectTimeoutElapsed = delegate { };

        private CBCentralManager CentralManager { get; set; }

        public bool IsScanning { get; private set; }

        public bool IsConnecting { get; private set; }

        public IList<IDevice> DiscoveredDevices { get; private set; } = new List<IDevice>();

        public IList<IDevice> ConnectedDevices { get; private set; } = new List<IDevice>();

        public static Adapter Current { get; private set; } = new Adapter();

        protected Adapter()
        {
            CentralManager = new CBCentralManager(DispatchQueue.CurrentQueue);

            CentralManager.DiscoveredPeripheral += (object sender, CBDiscoveredPeripheralEventArgs e) =>
            {
                Console.WriteLine("DiscoveredPeripheral: " + e.Peripheral.Name);
                Device d = new Device(e.Peripheral);
                if (!ContainsDevice(DiscoveredDevices, e.Peripheral))
                {
                    DiscoveredDevices.Add(d);
                    DeviceDiscovered(this, new DeviceDiscoveredEventArgs() { Device = d });
                }
            };

            CentralManager.UpdatedState += (object sender, EventArgs e) =>
            {
                Console.WriteLine("UpdatedState: " + CentralManager.State);
                _stateChanged.Set();
            };

            CentralManager.ConnectedPeripheral += (object sender, CBPeripheralEventArgs e) =>
            {
                Console.WriteLine("ConnectedPeripheral: " + e.Peripheral.Name);

                // When a peripheral gets connected, add that peripheral to our running list of 
                // connected peripherals
                if (!ContainsDevice(ConnectedDevices, e.Peripheral))
                {
                    Device d = new Device(e.Peripheral);
                    ConnectedDevices.Add(new Device(e.Peripheral));
                    // raise our connected event
                    DeviceConnected(sender, new DeviceConnectionEventArgs() { Device = d });
                }
            };

            CentralManager.DisconnectedPeripheral += (object sender, CBPeripheralErrorEventArgs e) =>
            {
                Console.WriteLine("DisconnectedPeripheral: " + e.Peripheral.Name);

                // when a peripheral disconnects, remove it from our running list.
                IDevice foundDevice = null;
                foreach (var d in ConnectedDevices)
                {
                    if (d.ID == Guid.ParseExact(e.Peripheral.Identifier.AsString(), "d"))
                        foundDevice = d;
                }

                if (foundDevice != null)
                    ConnectedDevices.Remove(foundDevice);

                // raise our disconnected event
                DeviceDisconnected(sender, new DeviceConnectionEventArgs() 
                                           { 
                                               Device = new Device(e.Peripheral) 
                                           });
            };

            CentralManager.FailedToConnectPeripheral += (object sender, CBPeripheralErrorEventArgs e) =>
            {
                // raise the failed to connect event
                DeviceFailedToConnect(this, new DeviceConnectionEventArgs()
                {
                    Device       = new Device(e.Peripheral),
                    ErrorMessage = e.Error.Description
                });
            };
        }

        public void StartScanningForDevices()
        {
            StartScanningForDevices(serviceUuid: Guid.Empty);
        }

        async Task WaitForState(CBCentralManagerState state)
        {
            Debug.WriteLine("Adapter: Waiting for state: " + state);

            while (CentralManager.State != state)
            {
                await Task.Run(() => _stateChanged.WaitOne());
            }
        }

        public async void StartScanningForDevices(Guid serviceUuid)
        {
            await WaitForState(CBCentralManagerState.PoweredOn);

            Debug.WriteLine("Adapter: Starting a scan for devices.");

            CBUUID[] serviceUuids = null; // TODO: convert to list so multiple Uuids can be detected
            if (serviceUuid != Guid.Empty)
            {
                var suuid    = CBUUID.FromString(serviceUuid.ToString());
                serviceUuids = new CBUUID[] { suuid };

                Debug.WriteLine("Adapter: Scanning for " + suuid);
            }

            // clear out the list
            DiscoveredDevices = new List<IDevice>();

            // start scanning
            IsScanning = true;
            CentralManager.ScanForPeripherals(serviceUuids);

            // in 10 seconds, stop the scan
            await Task.Delay(10000);

            // if we're still scanning
            if (IsScanning)
            {
                Console.WriteLine("Adapter: Scan timeout has elapsed.");
                IsScanning = false;
                CentralManager.StopScan();
                ScanTimeoutElapsed(this, new EventArgs());
            }
        }

        public void StopScanningForDevices()
        {
            Console.WriteLine("Adapter: Stopping the scan for devices.");
            IsScanning = false;
            CentralManager.StopScan();
        }

        public void ConnectToDevice(IDevice device)
        {
            //TODO: if it doesn't connect after 10 seconds, cancel the operation
            // (follow the same model we do for scanning).
            CentralManager.ConnectPeripheral(device.NativeDevice as CBPeripheral, new PeripheralConnectionOptions());

            /*
            // in 10 seconds, stop the connection
            await Task.Delay(10000);

            // if we're still trying to connect
            if (IsConnecting)
            {
                Console.WriteLine("Adapter: Connect timeout has elapsed.");
                Central. ...
                ConnectTimeoutElapsed(this, new EventArgs());
            }
            */
        }

        public void DisconnectDevice(IDevice device)
        {
            CentralManager.CancelPeripheralConnection(device.NativeDevice as CBPeripheral);
        }

        // util
        protected bool ContainsDevice(IEnumerable<IDevice> list, CBPeripheral device)
        {
            foreach (var d in list)
            {
                if (Guid.ParseExact(device.Identifier.AsString(), "d") == d.ID)
                    return true;
            }

            return false;
        }
    }
}

