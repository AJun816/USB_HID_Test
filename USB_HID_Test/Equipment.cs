using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USB_HID_Test
{
    public class Equipment
    {

        HIDInterface hid = new HIDInterface();

        struct connectStatusStruct
        {
            public bool preStatus;
            public bool curStatus;
        }

        connectStatusStruct connectStatus = new connectStatusStruct();

        //推送连接状态信息
        public delegate void isConnectedDelegate(bool isConnected);
        public isConnectedDelegate isConnectedFunc;


        //推送接收数据信息
        public delegate void PushReceiveDataDele(byte[] datas);
        public PushReceiveDataDele pushReceiveData;

        //第一步需要初始化，传入vid、pid，并开启自动连接
        public void Initial()
        {
            hid.StatusConnected = StatusConnected;
            hid.DataReceived = DataReceived;

            HIDInterface.HidDeviceInfo hidDevice = new HIDInterface.HidDeviceInfo();
            hidDevice.vID = 0x0951;
            hidDevice.pID = 0x16E5;
            hidDevice.serial = "";
            hid.AutoConnect(hidDevice);
        }

        //不使用则关闭
        public void Close()
        {
            hid.StopAutoConnect();
        }

        //发送数据
        public bool SendBytes(byte[] data)
        {

            return hid.Send(data);

        }

        public bool ReceiveBytes(byte[] data)
        {
            return hid.Receive(data);
        }

        //接受到数据
        public void DataReceived(object sender, byte[] e)
        {
            if (pushReceiveData != null)
                pushReceiveData(e);


        }

        //状态改变接收
        public void StatusConnected(object sender, bool isConnect)
        {
            connectStatus.curStatus = isConnect;
            if (connectStatus.curStatus == connectStatus.preStatus)  //connect
                return;
            connectStatus.preStatus = connectStatus.curStatus;

            if (connectStatus.curStatus)
            {
                isConnectedFunc(true);
                //ReportMessage(MessagesType.Message, "连接成功");
            }
            else //disconnect
            {
                isConnectedFunc(false);
                //ReportMessage(MessagesType.Error, "无法连接");
            }

        }
    }
}
