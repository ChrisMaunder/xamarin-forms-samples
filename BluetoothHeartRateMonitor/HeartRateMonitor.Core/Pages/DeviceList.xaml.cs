using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Robotics.Mobile.Core.Bluetooth.LE;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace HeartRateMonitor
{	
	public partial class DeviceList : ContentPage
	{	
		IAdapter _adapter;
		ObservableCollection<IDevice> _devices;

        public DeviceList(IAdapter adapter)
        {
            InitializeComponent();
            _adapter = adapter;

            _devices = new ObservableCollection<IDevice>();
            listView.ItemsSource = _devices;

            adapter.DeviceDiscovered += (object sender, DeviceDiscoveredEventArgs e) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    _devices.Add(e.Device);
                });
            };

            adapter.ScanTimeoutElapsed += (sender, e) =>
            {
                adapter.StopScanningForDevices(); // not sure why it doesn't stop already, if the timeout elapses... or is this a fake timeout we made?
                Device.BeginInvokeOnMainThread(() =>
                {
                    IsBusy = false;
                    DisplayAlert("Timeout", "Bluetooth scan timeout elapsed, no heart rate monitors were found", "OK");
                });
            };

            ScanHrmButton.Clicked += (sender, e) =>
            {
                InfoFrame.IsVisible = false;
                // this is the UUID for Heart Rate Monitors
                StartScanning(0x180D.UuidFromPartial());
            };
        }

        public void OnItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            ListView listView = sender as ListView;
            if (listView?.SelectedItem == null)
                return;

            Debug.WriteLine(" xxxxxxxxxxxx  OnItemSelected " + e.SelectedItem.ToString());
            IsBusy = false;
            StopScanning();

            var device      = e.SelectedItem as IDevice;
            var servicePage = new ServiceList(_adapter, device);
            // load services on the next page
            Navigation.PushAsync(servicePage);

            listView.SelectedItem = null; // clear selection
        }

        void StartScanning()
        {
            IsBusy = true;
            StartScanning(Guid.Empty);
        }

        void StartScanning(Guid forService)
        {
            if (_adapter.IsScanning)
            {
                IsBusy = false;
                _adapter.StopScanningForDevices();
                Debug.WriteLine("adapter.StopScanningForDevices()");
            }
            else
            {
                _devices.Clear();
                IsBusy = true;
                // _adapter.StartScanningForDevices(forService);
                _adapter.StartScanningForDevices();
                Debug.WriteLine("adapter.StartScanningForDevices(" + forService + ")");
            }
        }

        void StopScanning()
        {
            // stop scanning
            new Task(() =>
            {
                if (_adapter.IsScanning)
                {
                    Debug.WriteLine("Still scanning, stopping the scan");
                    _adapter.StopScanningForDevices();
                    IsBusy = false;
                }
            }).Start();
        }
	}
}
