using System;
using System.Collections.Generic;
using System.Text;

namespace LineMap
{
    public class DeviceConfiguration
    {

        public string IPAddress { get; set; }

        public int FifoInDBNumber { get; set; }

        public int FifoInPosDBNumber { get; set; }

        public int FifoOutDBNumber { get; set; }

        public int FifoOutPosDBNumber { get; set; }

        public int IDPlc { get; set; }

        public int IDDevice { get; set; }

    }
}
