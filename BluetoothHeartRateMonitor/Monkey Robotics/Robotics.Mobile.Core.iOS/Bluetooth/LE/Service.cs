using System;
using System.Collections.Generic;

#if __UNIFIED__
using CoreBluetooth;
#else
using MonoTouch.CoreBluetooth;
#endif

namespace Robotics.Mobile.Core.Bluetooth.LE
{
    public class Service : IService
    {
        public event EventHandler CharacteristicsDiscovered = delegate { };

        protected CBService    _nativeService;
        protected CBPeripheral _parentDevice;
        protected string       _name = null;

        protected IList<ICharacteristic> _characteristics;

        public Service(CBService nativeService, CBPeripheral parentDevice)
        {
            _nativeService = nativeService;
            _parentDevice = parentDevice;
        }

        public Guid ID
        {
            get { return _nativeService.UUID.ToString().GuidFromAssignedNumber(); }
        }

        public string Name
        {
            get
            {
                if (_name == null)
                    _name = KnownServices.Lookup(ID).Name;

                return _name;
            }
        }

        public bool IsPrimary
        {
            get { return _nativeService.Primary; }
        }

        // TODO: decide how to Interface this, Right now it's only in the iOS implementation
        public void DiscoverCharacteristics()
        {
            // TODO: need to raise the event and listen for it.
            _parentDevice.DiscoverCharacteristics(_nativeService);
        }

        public IList<ICharacteristic> Characteristics
        {
            get
            {
                // if it hasn't been populated yet, populate it
                if (_characteristics == null)
                {
                    _characteristics = new List<ICharacteristic>();
                    if (_nativeService.Characteristics != null)
                    {
                        foreach (var item in _nativeService.Characteristics)
                        {
                            _characteristics.Add(new Characteristic(item, _parentDevice));
                        }
                    }
                }

                return _characteristics;
            }
        }

        public void OnCharacteristicsDiscovered()
        {
            CharacteristicsDiscovered(this, new EventArgs());
        }

        public ICharacteristic FindCharacteristic(KnownCharacteristic characteristic)
        {
            // TODO: why don't we look in the internal list _chacateristics?
            foreach (var item in _nativeService.Characteristics)
            {
                if (string.Equals(item.UUID.ToString(), characteristic.ID.ToString()))
                    return new Characteristic(item, _parentDevice);
            }

            return null;
        }
    }
}

