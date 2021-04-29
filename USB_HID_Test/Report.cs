using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USB_HID_Test
{
    public class Report : EventArgs
    {
        public readonly byte reportID;
        public readonly byte[] reportBuff;
        public Report(byte id, byte[] arrayBuff)
        {
            reportID = id;
            reportBuff = arrayBuff;
        }
    }
}
