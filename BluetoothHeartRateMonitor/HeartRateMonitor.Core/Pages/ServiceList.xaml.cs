using System;
using Xamarin.Forms;
using Robotics.Mobile.Core.Bluetooth.LE;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace HeartRateMonitor
{
    public partial class ServiceList : ContentPage
    {
        IAdapter _adapter;
        IDevice  _device;

        ObservableCollection<IService> _services;

        public ServiceList(IAdapter adapter, IDevice device)
        {
            InitializeComponent();
            _adapter  = adapter;
            _device   = device;
            _services = new ObservableCollection<IService>();

            listView.ItemsSource = _services;

            // when device is connected
            adapter.DeviceConnected += (s, e) =>
            {
                device = e.Device; // do we need to overwrite this?

                // when services are discovered
                device.ServicesDiscovered += (object sender, EventArgs eventArgs) =>
                {
                    Debug.WriteLine("device.ServicesDiscovered");

                    // _services = (List<IService>)device.Services;
                    if (_services.Count == 0)
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            foreach (var service in device.Services)
                            {
                                _services.Add(service);
                            }
                        });
                    }

                    IsBusy = false;
                };

                // IsBusy = true; - causes crash

                // start looking for services
                device.DiscoverServices();
            };

            // TODO: add to IAdapter first
            // adapter.DeviceFailedToConnect += (sender, else) => {};

            DisconnectButton.Clicked += (sender, e) =>
            {
                adapter.DisconnectDevice(device);
                Navigation.PopToRootAsync(); // disconnect means start over
            };
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (_services.Count == 0)
            {
                Debug.WriteLine("No services, attempting to connect to device");
                // start looking for the device
                _adapter.ConnectToDevice(_device);
            }
        }
        public void OnItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            ListView listView = sender as ListView;
            if (listView?.SelectedItem == null)
                return;

            var service = e.SelectedItem as IService;
            var characteristicsPage = new CharacteristicList(_adapter, _device, service);
            Navigation.PushAsync(characteristicsPage);

            listView.SelectedItem = null; // clear selection
        }
    }
}

