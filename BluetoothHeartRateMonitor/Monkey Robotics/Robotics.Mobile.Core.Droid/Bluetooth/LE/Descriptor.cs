using System;
using Android.Bluetooth;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
	public class Descriptor : IDescriptor
	{
        protected string _name = null;
        protected BluetoothGattDescriptor _nativeDescriptor; 

		public /*BluetoothGattDescriptor*/ object NativeDescriptor
        {
            get { return _nativeDescriptor as object; }
        } 

		public Guid ID 
        {
            //return _nativeDescriptor.Uuid.ToString ();
            get { return Guid.ParseExact(_nativeDescriptor.Uuid.ToString(), "d"); }
		}

		public string Name
        {
            get
            {
                if (_name == null)
                    _name = KnownDescriptors.Lookup(ID).Name;

                return _name;
            }
        }

		public Descriptor (BluetoothGattDescriptor nativeDescriptor)
		{
			_nativeDescriptor = nativeDescriptor;
		}
	}
}

