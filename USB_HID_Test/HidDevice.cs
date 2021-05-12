using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace USB_HID_Test
{
    public class HidDevice : IDisposable
    {
        public enum MessagesType
        {
            Message,
            Error
        }

        public struct ReusltString
        {
            public bool Result;
            public string message;
        }

        public struct HidDeviceInfo
        {
            public UInt16 vID;
            public UInt16 pID;
            public string serial;
        }

        struct ConnectStatusStruct
        {
            public bool preStatus;
            public bool curStatus;
        }

        ConnectStatusStruct connectStatus = new ConnectStatusStruct();

        //推送连接状态信息
        public delegate void isConnectedDelegate(bool isConnected);
        public isConnectedDelegate isConnectedFunc;

        //推送接收数据信息
        public delegate void PushReceiveDataDele(byte[] datas);
        public PushReceiveDataDele pushReceiveData;

        HidDeviceInfo lowHidDevice = new HidDeviceInfo();

        public delegate void DelegateDataReceived(object sender, byte[] data);
        public DelegateDataReceived DataReceived;

        public delegate void DelegateStatusConnected(object sender, bool isConnect);
        public DelegateStatusConnected StatusConnected;

        public bool bConnected = false;

        public HidDeviceBase device = new HidDeviceBase();
        private static HidDevice m_oInstance;

        Boolean ContinueConnectFlag = true;
        private readonly BackgroundWorker ReadWriteThread = new BackgroundWorker();

        public struct TagInfo
        {
            public string AntennaPort;
            public string EPC;
        }

        public HidDevice()
        {
            m_oInstance = this;
            device.DataReceived = HidDataReceived;
            device.DeviceRemoved = HidDeviceRemoved;
        }

        protected virtual void RaiseEventConnectedState(bool isConnect)
        {
            StatusConnected?.Invoke(this, isConnect);
        }

        protected virtual void RaiseEventDataReceived(byte[] buf)
        {
            DataReceived?.Invoke(this, buf);
        }

        public void Initial(UInt16 vID, UInt16 pID, string serial)
        {
            StatusConnected = StatusConnectedDel;
            DataReceived = DataReceiveds;

            HidDevice.HidDeviceInfo hidDevice = new()
            {
                vID = vID,
                pID = pID,
                serial = serial
            };
            AutoConnect(hidDevice);
        }
        public void AutoConnect(HidDeviceInfo hidDevice)
        {
            lowHidDevice = hidDevice;
            ContinueConnectFlag = true;
            ReadWriteThread.DoWork += ReadWriteThread_DoWork;
            ReadWriteThread.WorkerSupportsCancellation = true;
            ReadWriteThread.RunWorkerAsync();
        }

        public void StopAutoConnect()
        {
            try
            {
                ContinueConnectFlag = false;
                Dispose();
            }
            catch
            {

            }
        }

        public void StatusConnectedDel(object sender, bool isConnect)
        {
            connectStatus.curStatus = isConnect;
            if (connectStatus.curStatus == connectStatus.preStatus)  //connect
                return;
            connectStatus.preStatus = connectStatus.curStatus;

            if (connectStatus.curStatus)
            {
                isConnectedFunc(true);
                Console.WriteLine("连接成功");
            }
            else
            {
                isConnectedFunc(false);
                Console.WriteLine("无法连接");
            }

        }   

        public bool Connect(HidDeviceInfo hidDevice)
        {
            ReusltString result = new ReusltString();

            HidDeviceData.HID_RETURN hdrtn = device.OpenDevice(hidDevice.vID, hidDevice.pID, hidDevice.serial);

            if (hdrtn == HidDeviceData.HID_RETURN.SUCCESS)
            {
                bConnected = true;

                #region 消息通知
                result.Result = true;
                result.message = "设备连接成功!";
                RaiseEventConnectedState(result.Result);
                #endregion

                return true;
            }

            bConnected = false;

            #region 消息通知
            result.Result = false;
            result.message = "设备连接错误";
            RaiseEventConnectedState(result.Result);

            #endregion
            return false;
        }

        public bool ReadFeature(byte[] byData, byte reportId = 0)
        {
            HidDeviceData.HID_RETURN hdrtn = device.SetFeature(new HidDeviceReport(reportId, byData));

            if (hdrtn != HidDeviceData.HID_RETURN.SUCCESS)
            {
                return false;
            }
            return true;
        }

        public bool WriteFeature(byte reportId, byte[] byData)
        {
            HidDeviceData.HID_RETURN hdrtn = device.GetFeature(new HidDeviceReport(reportId, byData));
            if (hdrtn != HidDeviceData.HID_RETURN.SUCCESS)
            {
                return false;
            }
            return true;
        }

        public bool ReadFeatureData(byte[] data)
        {
            return ReadFeature(data);
        }

        public bool WriteFeatureData( byte[] data,byte reportId=0)
        {
            return WriteFeature(reportId, data);
        }

        public void DataReceiveds(object sender, byte[] e)
        {
            pushReceiveData?.Invoke(e);
        }  

        void HidDeviceRemoved(object sender, EventArgs e)
        {
            bConnected = false;
            #region 消息通知
            ReusltString result = new ReusltString();
            result.Result = false;
            result.message = "Device Remove";
            RaiseEventConnectedState(result.Result);
            #endregion
            if (device != null)
            {
                device.CloseDevice();
            }
        }

        public void HidDataReceived(object sender, HidDeviceReport e)
        {
            try
            {
                //第一个字节为数据长度，因为Device 的HID数据固定长度为64字节，取有效数据
                byte[] buf = new byte[e.reportBuff[0]];
                Array.Copy(e.reportBuff, 1, buf, 0, e.reportBuff[0]);

                //推送数据
                RaiseEventDataReceived(buf);
            }
            catch
            {
                #region 消息通知
                ReusltString result = new ReusltString();
                result.Result = false;
                result.message = "Receive Error";
                RaiseEventConnectedState(result.Result);
                #endregion
            }
        }

        private void ReadWriteThread_DoWork(object sender, DoWorkEventArgs e)
        {
            while (ContinueConnectFlag)
            {
                try
                {
                    if (!bConnected)
                    {
                        Connect(lowHidDevice);
                    }
                    Thread.Sleep(500);
                }
                catch (Exception ex) { }
            }
        }

        public void Close()
        {
            StopAutoConnect();
        }

        public void DisConnect()
        {
            bConnected = false;

            Thread.Sleep(200);
            if (device != null)
            {
                device.CloseDevice();
            }
        }

        public void Dispose()
        {
            try
            {
                this.DisConnect();
                device.DataReceived -= HidDataReceived;
                device.DeviceRemoved -= HidDeviceRemoved;
                ReadWriteThread.DoWork -= ReadWriteThread_DoWork;
                ReadWriteThread.CancelAsync();
                ReadWriteThread.Dispose();
            }
            catch
            { }
        }

        ~HidDevice()
        {
            Dispose();
        }
    }
}
