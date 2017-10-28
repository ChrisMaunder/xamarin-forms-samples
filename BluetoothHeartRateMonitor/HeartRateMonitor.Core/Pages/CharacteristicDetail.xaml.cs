﻿using Xamarin.Forms;
using Robotics.Mobile.Core.Bluetooth.LE;
using System.Diagnostics;
using System.Linq;

namespace HeartRateMonitor
{
    public partial class CharacteristicDetail : ContentPage
    {
        ICharacteristic _characteristic;

        public CharacteristicDetail(IAdapter adapter, IDevice device, IService service, 
                                    ICharacteristic characteristic)
        {
            InitializeComponent();
            _characteristic = characteristic;

            if (characteristic.CanUpdate)
            {
                characteristic.ValueUpdated += (s, e) =>
                {
                    Debug.WriteLine("characteristic.ValueUpdated");
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        IsBusy = false; // only spin until the first result is received
                        UpdateDisplay(characteristic);
                    });

                };

                IsBusy = true;
                characteristic.StartUpdates();
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_characteristic.CanRead)
            {
                var c = await _characteristic.ReadAsync();
                UpdateDisplay(c);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (_characteristic.CanUpdate)
                _characteristic.StopUpdates();
        }

        void UpdateDisplay(ICharacteristic c)
        {
            Name.Text = c.Name;

            //ID.Text = c.ID.ToString();
            ID.Text = c.ID.AssignedNumberFromGuid();

            var s = (from i in c.Value select i.ToString("X").PadRight(2, '0')).ToArray();
            RawValue.Text = string.Join(":", s);

            if (c.ID == 0x2A37.GuidFromAssignedNumber())
            {
                // heart rate
                StringValue.Text      = DecodeHeartRateCharacteristicValue(c.Value);
                StringValue.TextColor = Color.Red;
            }
            else
            {
                StringValue.Text      = c.StringValue;
                StringValue.TextColor = Color.Default;
            }
        }

        string DecodeHeartRateCharacteristicValue(byte[] data)
        {
            ushort bpm = 0;
            if ((data[0] & 0x01) == 0)
            {
                bpm = data[1];
            }
            else
            {
                bpm = (ushort)data[1];
                bpm = (ushort)(((bpm >> 8) & 0xFF) | ((bpm << 8) & 0xFF00));
            }

            return bpm.ToString() + " bpm";
        }
    }
}

