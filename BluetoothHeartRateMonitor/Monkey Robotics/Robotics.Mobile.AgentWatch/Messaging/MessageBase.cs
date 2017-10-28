using System;
using System.Collections.Generic;

namespace Android.Robotics.Messaging
{
    public class MessageBase
    {
        public MessageBase()
        {
            Data = new Dictionary<string, object>();
        }

        public Dictionary<string, object> Data { get; set; }
    }
}

