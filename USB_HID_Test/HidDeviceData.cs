using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USB_HID_Test
{
    public class HidDeviceData
    {
        /// <summary>
        /// 连接USB返回的结构体信息
        /// </summary>
        public enum HID_RETURN
        {
            SUCCESS = 0,
            NO_DEVICE_CONECTED,
            DEVICE_NOT_FIND,
            DEVICE_OPENED,
            WRITE_FAILD,
            READ_FAILD
        }
    }
}
