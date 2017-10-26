using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Robotics.Mobile.Core.Utils;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
    // Source: https://developer.bluetooth.org/gatt/services/Pages/ServicesHome.aspx
    public static class KnownServices
    {
        private static Dictionary<Guid, KnownService> _items;
        private static object _lock = new object();

        static KnownServices()
        {
        }

        public static KnownService Lookup(Guid id)
        {
            lock (_lock)
            {
                if (_items == null)
                    LoadItemsFromJson();
            }

            if (_items.ContainsKey(id))
                return _items[id];

            return new KnownService { Name = "Unknown", ID = Guid.Empty };
        }

        public static void LoadItemsFromJson()
        {
            _items = new Dictionary<Guid, KnownService>();

            //TODO: switch over to ServiceStack.Text when it gets bound.

            Assembly assembly = typeof(KnownServices).GetTypeInfo().Assembly;
            string items = ResourceLoader.GetEmbeddedResourceString(assembly, "KnownServices.json");

            JToken json = JValue.Parse(items);
            foreach (JToken item in json.Children())
            {
                JProperty prop = item as JProperty;
                var service = new KnownService() 
                { 
                    Name = prop.Value.ToString(), 
                    ID   = Guid.ParseExact(prop.Name, "d") 
                };

                _items.Add(service.ID, service);
            }
        }
    }

    public struct KnownService
    {
        public string Name;
        public Guid   ID;
    }
}

