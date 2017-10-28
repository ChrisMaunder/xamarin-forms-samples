using System;
using Xamarin.Forms;
using Robotics.Mobile.Core.Bluetooth.LE;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace HeartRateMonitor
{	
	public partial class CharacteristicList : ContentPage
	{	
		IAdapter _adapter;
		IDevice  _device;
		IService _service; 

		ObservableCollection<ICharacteristic> _characteristics;

        public CharacteristicList(IAdapter adapter, IDevice device, IService service)
        {
            InitializeComponent();
            
            _adapter = adapter;
            _device  = device;
            _service = service;

            _characteristics = new ObservableCollection<ICharacteristic>();

            listView.ItemsSource = _characteristics;

            // when characteristics are discovered
            service.CharacteristicsDiscovered += (object sender, EventArgs e) =>
            {
                Debug.WriteLine("service.CharacteristicsDiscovered");
                if (_characteristics.Count == 0)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        foreach (var characteristic in service.Characteristics)
                        {
                            _characteristics.Add(characteristic);
                        }
                    });
                }
            };

            // start looking for characteristics
            service.DiscoverCharacteristics();
        }

		protected override void OnAppearing ()
		{
			base.OnAppearing ();
			
            if (_characteristics.Count == 0) 
            {
				Debug.WriteLine ("No characteristics, attempting to find some");
				// start looking for the device
				_adapter.ConnectToDevice (_device); 
			}
		}

        /// <summary>
        /// Display a Characteristics Page
        /// </summary>
        public void OnItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            ListView listView = sender as ListView;
            if (listView?.SelectedItem == null)
                return;

            var characteristic = e.SelectedItem as ICharacteristic;
            ContentPage characteristicsPage = null;

            if (characteristic.ID == 0x2A37.GuidFromAssignedNumber() || 
                characteristic.ID == 0x2A38.GuidFromAssignedNumber())
            {
                characteristicsPage = new CharacteristicDetail_Hrm(_adapter, _device, _service, 
                                                                   characteristic);
            }
            else
            {
                characteristicsPage = new CharacteristicDetail(_adapter, _device, _service, 
                                                               characteristic);
            }

            Navigation.PushAsync(characteristicsPage);

            listView.SelectedItem = null; // clear selection
        }
	}
}

