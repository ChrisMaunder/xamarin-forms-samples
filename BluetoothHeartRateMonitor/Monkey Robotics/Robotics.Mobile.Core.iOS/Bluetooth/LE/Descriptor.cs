using System;
using CoreBluetooth;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
    public class Descriptor : IDescriptor
    {
        protected CBDescriptor _nativeDescriptor;
        protected string       _name = null;

        public /*CBDescriptor*/ object NativeDescriptor
        {
            get { return _nativeDescriptor as object; }
        }

        public Guid ID
        {
            get { return Guid.ParseExact(_nativeDescriptor.UUID.ToString(), "d"); }
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

        public Descriptor(CBDescriptor nativeDescriptor)
        {
            _nativeDescriptor = nativeDescriptor;
        }
    }
}

