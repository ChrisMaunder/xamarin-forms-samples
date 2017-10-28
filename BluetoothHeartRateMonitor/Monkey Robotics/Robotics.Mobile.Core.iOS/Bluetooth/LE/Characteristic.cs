using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using CoreBluetooth;
using Foundation;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
    public class Characteristic : ICharacteristic
    {
        public event EventHandler<CharacteristicReadEventArgs> ValueUpdated = delegate { };

        protected CBCharacteristic _nativeCharacteristic;
        protected IList<IDescriptor> _descriptors;

        private CBPeripheral _parentDevice;

        public Characteristic(CBCharacteristic nativeCharacteristic, CBPeripheral parentDevice)
        {
            _nativeCharacteristic = nativeCharacteristic;
            _parentDevice = parentDevice;
        }

        /// <summary>
        /// The assigned number of this characteristic.
        /// </summary>
        public string Uuid
        {
            get { return _nativeCharacteristic.UUID.ToString(); }
        }

        /// <summary>
        /// The full GUID based on the assigned number of this characteristic.
        /// </summary>
        public Guid ID
        {
            get { return _nativeCharacteristic.UUID.ToString().GuidFromAssignedNumber(); }
        }

        public byte[] Value
        {
            get
            {
                if (_nativeCharacteristic.Value == null)
                    return null;

                return _nativeCharacteristic.Value.ToArray();
            }
        }

        public string StringValue
        {
            get
            {
                if (Value == null)
                    return string.Empty;

                byte[] stringBytes = Value;

                var utf8 = System.Text.Encoding.UTF8.GetString(stringBytes);
                //var ascii = System.Text.Encoding.ASCII.GetString (stringByes);

                return utf8;
            }
        }

        public string Name
        {
            get { return KnownCharacteristics.Lookup(ID).Name; }
        }

        public CharacteristicPropertyType Properties
        {
            get { return (CharacteristicPropertyType)(int)_nativeCharacteristic.Properties; }
        }

        public IList<IDescriptor> Descriptors
        {
            get
            {
                // if we haven't converted them to our xplat objects
                if (_descriptors != null)
                {
                    _descriptors = new List<IDescriptor>();

                    // convert the internal list of them to the xplat ones
                    foreach (var item in _nativeCharacteristic.Descriptors)
                    {
                        _descriptors.Add(new Descriptor(item));
                    }
                }

                return _descriptors;
            }
        }

        public object NativeCharacteristic
        {
            get { return _nativeCharacteristic; }
        }

        public bool CanRead
        {
            get { return (Properties & CharacteristicPropertyType.Read) != 0; }
        }

        public bool CanUpdate
        {
            get { return (Properties & CharacteristicPropertyType.Notify) != 0; }
        }

        public bool CanWrite
        {
            get { return (Properties & (CharacteristicPropertyType.WriteWithoutResponse | 
                                        CharacteristicPropertyType.AppleWriteWithoutResponse)) != 0; }
        }

        public Task<ICharacteristic> ReadAsync()
        {
            var completionSource = new TaskCompletionSource<ICharacteristic>();

            if (!CanRead)
                throw new InvalidOperationException("Characteristic does not support READ");

            EventHandler<CBCharacteristicEventArgs> updated = null;
            updated = (object sender, CBCharacteristicEventArgs e) =>
            {
                Console.WriteLine(".....UpdatedCharacterteristicValue");

                var characteristic = new Characteristic(e.Characteristic, _parentDevice);
                completionSource.SetResult(characteristic);

                _parentDevice.UpdatedCharacterteristicValue -= updated;
            };

            _parentDevice.UpdatedCharacterteristicValue += updated;
            Console.WriteLine(".....ReadAsync");
            _parentDevice.ReadValue(_nativeCharacteristic);

            return completionSource.Task;
        }

        public void Write(byte[] data)
        {
            if (!CanWrite)
                throw new InvalidOperationException("Characteristic does not support WRITE");

            var nsdata = NSData.FromArray(data);
            var descriptor = (CBCharacteristic)_nativeCharacteristic;

            var writeType = (Properties & CharacteristicPropertyType.AppleWriteWithoutResponse) != 0 
                          ? CBCharacteristicWriteType.WithoutResponse 
                          : CBCharacteristicWriteType.WithResponse;

            _parentDevice.WriteValue(nsdata, descriptor, writeType);

            // Console.WriteLine ("** Characteristic.Write, Type = " + t + ", Data = " + BitConverter.ToString (data));
        }

        public void StartUpdates()
        {
            // TODO: should be bool RequestValue? compare iOS API for commonality
            bool successful = false;
            if (CanRead)
            {
                Console.WriteLine("** Characteristic.RequestValue, PropertyType = Read, requesting read");
                _parentDevice.UpdatedCharacterteristicValue += UpdatedRead;

                _parentDevice.ReadValue(_nativeCharacteristic);

                successful = true;
            }

            if (CanUpdate)
            {
                Console.WriteLine("** Characteristic.RequestValue, PropertyType = Notify, requesting updates");
                _parentDevice.UpdatedCharacterteristicValue += UpdatedNotify;

                _parentDevice.SetNotifyValue(true, _nativeCharacteristic);

                successful = true;
            }

            Console.WriteLine("** RequestValue, Succesful: " + successful.ToString());
        }

        public void StopUpdates()
        {
            if (CanUpdate)
            {
                _parentDevice.SetNotifyValue(false, _nativeCharacteristic);
                Console.WriteLine("** Characteristic.RequestValue, PropertyType = Notify, STOP updates");
            }
        }

        // removes listener after first response received
        void UpdatedRead(object sender, CBCharacteristicEventArgs e)
        {
            ValueUpdated(this, new CharacteristicReadEventArgs()
                               {
                                   Characteristic = new Characteristic(e.Characteristic, _parentDevice)
                               });

            _parentDevice.UpdatedCharacterteristicValue -= UpdatedRead;
        }

        // continues to listen indefinitely
        void UpdatedNotify(object sender, CBCharacteristicEventArgs e)
        {
            ValueUpdated(this, new CharacteristicReadEventArgs()
                                {
                                    Characteristic = new Characteristic(e.Characteristic, _parentDevice)
                                });
        }
    }
}

