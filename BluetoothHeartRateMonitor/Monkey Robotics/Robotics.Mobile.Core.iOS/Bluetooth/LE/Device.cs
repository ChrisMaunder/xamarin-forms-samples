using System;
using System.Collections.Generic;

using CoreBluetooth;
using Foundation;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
    public class Device : DeviceBase
    {
        public override event EventHandler ServicesDiscovered = delegate { };

        protected CBPeripheral _nativeDevice;
        protected int _rssi;
        protected IList<IService> _services = new List<IService>();

        public Device(CBPeripheral nativeDevice)
        {
            _nativeDevice = nativeDevice;

            _nativeDevice.DiscoveredService += (object sender, NSErrorEventArgs e) =>
            {
                // why we have to do this check is beyond me. if a service has been discovered, the
                // collection shouldn't be null, but sometimes it is. le sigh, apple.
                if (_nativeDevice.Services != null)
                {
                    foreach (CBService s in _nativeDevice.Services)
                    {
                        Console.WriteLine("Device.Discovered Service: " + s.Description);
                        if (!ServiceExists(s))
                            _services.Add(new Service(s, _nativeDevice));
                    }

                    ServicesDiscovered(this, new EventArgs());
                }
            };

            // fixed for Unified https://bugzilla.xamarin.com/show_bug.cgi?id=14893
            _nativeDevice.DiscoveredCharacteristic += (object sender, CBServiceEventArgs e) =>
            {
                Console.WriteLine("Device.DiscoveredCharacteristic.");

                //loop through each service, and update the characteristics
                foreach (CBService srv in ((CBPeripheral)sender).Services)
                {
                    // if the service has characteristics yet
                    if (srv.Characteristics != null)
                    {

                        // locate the our new service
                        foreach (var item in Services)
                        {
                            // if we found the service
                            if (item.ID == srv.UUID.ToString().GuidFromAssignedNumber())
                            {
                                item.Characteristics.Clear();

                                // add the discovered characteristics to the particular service
                                foreach (var characteristic in srv.Characteristics)
                                {
                                    Console.WriteLine("Characteristic: " + characteristic.Description);
                                    Characteristic newChar = new Characteristic(characteristic, _nativeDevice);
                                    item.Characteristics.Add(newChar);
                                }

                                // inform the service that the characteristics has been discovered
                                // TODO: really, we should just be using a notifying collection.
                                (item as Service).OnCharacteristicsDiscovered();
                            }
                        }
                    }
                }
            };
        }

        public override Guid ID
        {
            get
            {
                // TODO: not sure if this is right. hell, not even sure if a device should have a 
                // UDDI. iOS BLE peripherals do, though. Need to look at the BLE Spec.
                // Actually.... deprecated in iOS7!
                // Actually again, Uuid is, but Identifier isn't.
                // return _nativeDevice.Identifier.AsString ();//.ToString();

                return Guid.ParseExact(_nativeDevice.Identifier.AsString(), "d");
            }
        }

        public override string Name
        {
            get { return _nativeDevice.Name; }
        }

        public override int Rssi
        {
            get { return _rssi; }
        }

        public override object NativeDevice
        {
            get { return _nativeDevice; }
        }

        // TODO: investigate the validity of this. Android API seems to indicate that the bond 
        // state is available, rather than the connected state, which are two different things. 
        // You can be bonded but not connected.
        public override DeviceState State
        {
            get { return GetState(); }
        }

        public override IList<IService> Services
        {
            get { return _services; }
        }

        #region public methods --------------------------------------------------------------------

        public override void DiscoverServices()
        {
            _nativeDevice.DiscoverServices();
        }

        public void Disconnect()
        {
            Adapter.Current.DisconnectDevice(this);
            _nativeDevice.Dispose();
        }

        #endregion --------------------------------------------------------------------------------

        #region internal methods ------------------------------------------------------------------

        protected DeviceState GetState()
        {
            switch (_nativeDevice.State)
            {
                case CBPeripheralState.Connected:
                    return DeviceState.Connected;

                case CBPeripheralState.Connecting:
                    return DeviceState.Connecting;

                case CBPeripheralState.Disconnected:
                    return DeviceState.Disconnected;

                default:
                    return DeviceState.Disconnected;
            }
        }

        protected bool ServiceExists(CBService service)
        {
            foreach (var s in _services)
            {
                if (s.ID == service.UUID.ToString().GuidFromAssignedNumber())
                    return true;
            }

            return false;
        }

        #endregion --------------------------------------------------------------------------------
    }
}

