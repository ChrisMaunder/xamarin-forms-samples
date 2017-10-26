using System;
using System.Collections.Generic;
using Android.Bluetooth;
using System.Linq;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
	public class Device : DeviceBase
	{
		public override event EventHandler ServicesDiscovered = delegate {};

		protected BluetoothDevice _nativeDevice;
        protected int _rssi;
        protected IList<IService> _services = new List<IService>();

        /// <summary>
        /// we have to keep a reference to this because Android's api is weird and requires
        /// the GattServer in order to do nearly anything, including enumerating services
        /// 
        /// TODO: consider wrapping the Gatt and Callback into a single object and passing that 
        /// around instead.
        /// </summary>
        protected BluetoothGatt _gatt;

		/// <summary>
		/// we also track this because of gogole's weird API. the gatt callback is where
		/// we'll get notified when services are enumerated
		/// </summary>
		protected GattCallback _gattCallback;

        public Device(BluetoothDevice nativeDevice, BluetoothGatt gatt,
            GattCallback gattCallback, int rssi) : base()
        {
            _nativeDevice = nativeDevice;
            _gatt         = gatt;
            _gattCallback = gattCallback;
            _rssi         = rssi;

            // when the services are discovered on the gatt callback, cache them here
            if (_gattCallback != null)
            {
                _gattCallback.ServicesDiscovered += (s, e) =>
                {
                    var services = _gatt.Services;
                    _services = new List<IService>();
                    foreach (var item in services)
                    {
                        _services.Add(new Service(item, _gatt, _gattCallback));
                    }

                    ServicesDiscovered(this, e);
                };
            }
        }

		public override Guid ID
        {
            get
            {
                //TODO: verify - fix from Evolve player
                Byte[] deviceGuid = new Byte[16];
                String macWithoutColons = _nativeDevice.Address.Replace(":", "");
                Byte[] macBytes = Enumerable.Range(0, macWithoutColons.Length)
                                            .Where(x => x % 2 == 0)
                                            .Select(x => Convert.ToByte(macWithoutColons.Substring(x, 2), 16))
                                            .ToArray();
                macBytes.CopyTo(deviceGuid, 10);
                return new Guid(deviceGuid);
                //return _nativeDevice.Address;
                //return Guid.Empty;
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

		// TODO: investigate the validity of  Android API seems to indicate that the
		// bond state is available, rather than the connected state, which are two different 
		// things. you can be bonded but not connected.
		public override DeviceState State 
        {
			get { return GetState (); }
		}

		//TODO: strongly type IService here
		public override IList<IService> Services
		{
			get { return _services; }
		}

		#region public methods 

		public override void DiscoverServices ()
		{
			_gatt.DiscoverServices ();
		}

		public void Disconnect ()
		{
			_gatt.Disconnect ();
			_gatt.Dispose ();
		}

		#endregion

		#region internal methods

		protected DeviceState GetState()
		{
			switch (_nativeDevice.BondState) {
			    case Bond.Bonded:
				    return DeviceState.Connected;
			    case Bond.Bonding:
				    return DeviceState.Connecting;
			    case Bond.None:
			    default:
				    return DeviceState.Disconnected;
			}
		}

		#endregion
	}
}

