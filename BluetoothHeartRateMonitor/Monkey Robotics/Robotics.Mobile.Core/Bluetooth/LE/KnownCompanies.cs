using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Robotics.Mobile.Core.Utils;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
    // Source: https://www.bluetooth.com/specifications/assigned-numbers/company-identifiers
    public static class KnownCompanies
    {
        private static Dictionary<uint, KnownCompany> _items;
        private static object _lock = new object();

        static KnownCompanies()
        {
        }

        public static KnownCompany Lookup(uint id)
        {
            lock (_lock)
            {
                if (_items == null)
                    LoadItemsFromJson();
            }

            if (_items.ContainsKey(id))
                return _items[id];

            return new KnownCompany { Name = "Unknown", ID = 0 };
        }

        public static void LoadItemsFromJson()
        {
            _items = new Dictionary<uint, KnownCompany>();

            // TODO: switch over to CompanyStack.Text when it gets bound.

            Assembly assembly = typeof(KnownCompany).GetTypeInfo().Assembly;
            string items = ResourceLoader.GetEmbeddedResourceString(assembly, "KnownCompanies.json");

            JToken json = JValue.Parse(items);
            foreach (JToken item in json.Children())
            {
                JProperty prop = item as JProperty;
                var company = new KnownCompany() 
                { 
                    Name = prop.Value.ToString(), 
                    ID   = uint.Parse(prop.Name, System.Globalization.NumberStyles.HexNumber)
                };

                _items.Add(company.ID, company);
            }
        }
    }

    public struct KnownCompany
    {
        public string Name;
        public uint   ID;
    }
}

