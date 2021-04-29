using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static USB_HID_Test.NativeMethods;

namespace USB_HID_Test
{
 
    public class HidDevice : object
    {
        private IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        private const int MAX_USB_DEVICES = 64;
        private bool deviceOpened = false;
        private FileStream hidDevice = null;
        private IntPtr hHubDevice;

        int outputReportLength;//输出报告长度,包刮一个字节的报告ID
        public int OutputReportLength { get { return outputReportLength; } }
        int inputReportLength;//输入报告长度,包刮一个字节的报告ID   
        public int InputReportLength { get { return inputReportLength; } }
        IntPtr device = IntPtr.Zero;
        /// <summary>
        /// 打开指定信息的设备
        /// </summary>
        /// <param name="vID">设备的vID</param>
        /// <param name="pID">设备的pID</param>
        /// <param name="serial">设备的serial</param>
        /// <returns></returns>
        public HidDeviceData.HID_RETURN OpenDevice(UInt16 vID, UInt16 pID, string serial)
        {
            if (deviceOpened == false)
            {
                //获取连接的HID列表
                List<string> deviceList = new List<string>();
                GetHidDeviceList(deviceList);

                if (deviceList.Count == 0)
                    return HidDeviceData.HID_RETURN.NO_DEVICE_CONECTED;
                for (int i = 0; i < deviceList.Count; i++)
                {
                
                    device =NativeMethods.CreateFile(deviceList[i],
                                                DESIREDACCESS.GENERIC_READ | DESIREDACCESS.GENERIC_WRITE,
                                                0,
                                                0,
                                                CREATIONDISPOSITION.OPEN_EXISTING,
                                                FLAGSANDATTRIBUTES.FILE_FLAG_OVERLAPPED,
                                                0);
                    if (device != INVALID_HANDLE_VALUE)
                    {
                        HIDD_ATTRIBUTES attributes;
                        IntPtr serialBuff = Marshal.AllocHGlobal(512);
                        HidD_GetAttributes(device, out attributes);
                        HidD_GetSerialNumberString(device, serialBuff, 512);
                        string deviceStr = Marshal.PtrToStringAuto(serialBuff);
                        Marshal.FreeHGlobal(serialBuff);
                        if (attributes.VendorID == vID && attributes.ProductID == pID && deviceStr.Contains(serial))
                        {
                            IntPtr preparseData;
                            HIDP_CAPS caps;
                            HidD_GetPreparsedData(device, out preparseData);
                            HidP_GetCaps(preparseData, out caps);
                            HidD_FreePreparsedData(preparseData);
                            outputReportLength = caps.OutputReportByteLength;
                            inputReportLength = caps.InputReportByteLength;
                            //inputReportLength = 1;
                            try
                            {
                                hidDevice = new FileStream(new SafeFileHandle(device, false), FileAccess.ReadWrite, inputReportLength, true);
                            }
                            catch (Exception ex) { continue; }
                            deviceOpened = true;
                            BeginAsyncRead();

                            hHubDevice = device;
                            return HidDeviceData.HID_RETURN.SUCCESS;
                        }
                    }
                }
                return HidDeviceData.HID_RETURN.DEVICE_NOT_FIND;
            }
            else
                return HidDeviceData.HID_RETURN.DEVICE_OPENED;
        }

        /// <summary>
        /// 关闭打开的设备
        /// </summary>
        public void CloseDevice()
        {
            if (deviceOpened == true)
            {
                deviceOpened = false;
                hidDevice.Close();
            }
        }

        /// <summary>
        /// 开始一次异步读
        /// </summary>
        private void BeginAsyncRead()
        {
            byte[] inputBuff = new byte[InputReportLength];
            hidDevice.BeginRead(inputBuff, 0, InputReportLength, new AsyncCallback(ReadCompleted), inputBuff);
        }

        /// <summary>
        /// 异步读取结束,发出有数据到达事件
        /// </summary>
        /// <param name="iResult">这里是输入报告的数组</param>
        private void ReadCompleted(IAsyncResult iResult)
        {
            byte[] readBuff = (byte[])(iResult.AsyncState);
            try
            {
                hidDevice.EndRead(iResult);//读取结束,如果读取错误就会产生一个异常
                byte[] reportData = new byte[readBuff.Length - 1];
                for (int i = 1; i < readBuff.Length; i++)
                    reportData[i - 1] = readBuff[i];
                HidDeviceReport hid = new HidDeviceReport(readBuff[0], reportData);
                OnDataReceived(hid); //发出数据到达消息
                if (!deviceOpened) return;
                BeginAsyncRead();//启动下一次读操作
            }
            catch //读写错误,设备已经被移除
            {
                //MyConsole.WriteLine("设备无法连接，请重新插入设备");
                EventArgs ex = new EventArgs();
                OnDeviceRemoved(ex);//发出设备移除消息
                CloseDevice();

            }
        }

        public delegate void DelegateDataReceived(object sender, HidDeviceReport e);
        //public event EventHandler<ConnectEventArg> StatusConnected;

        public DelegateDataReceived DataReceived;

        /// <summary>
        /// 事件:数据到达,处理此事件以接收输入数据
        /// </summary>
        protected virtual void OnDataReceived(HidDeviceReport e)
        {
            if (DataReceived != null) DataReceived(this, e);
        }

        /// <summary>
        /// 事件:设备断开
        /// </summary>

        public delegate void DelegateStatusConnected(object sender, EventArgs e);
        public DelegateStatusConnected DeviceRemoved;
        protected virtual void OnDeviceRemoved(EventArgs e)
        {
            if (DeviceRemoved != null) DeviceRemoved(this, e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public HidDeviceData.HID_RETURN SetFeature(HidDeviceReport r)
        {
            if (deviceOpened)
            {
                try
                {
                    byte[] buffer = new byte[outputReportLength];
                    buffer[0] = r.reportID;
                    int maxBufferLength = 0;
                    if (r.reportBuff.Length < outputReportLength - 1)
                        maxBufferLength = r.reportBuff.Length;
                    else
                        maxBufferLength = outputReportLength - 1;

                    for (int i = 0; i < maxBufferLength; i++)
                        buffer[i + 1] = r.reportBuff[i];
     
                    bool isSetFeature = HidD_SetFeature(device, buffer, OutputReportLength);
                
                    if (isSetFeature) return HidDeviceData.HID_RETURN.SUCCESS;
                    return HidDeviceData.HID_RETURN.NO_DEVICE_CONECTED;
                }
                catch
                {
                    EventArgs ex = new EventArgs();
                    OnDeviceRemoved(ex);//发出设备移除消息
                    CloseDevice();
                    return HidDeviceData.HID_RETURN.NO_DEVICE_CONECTED;
                }
            }
            return HidDeviceData.HID_RETURN.WRITE_FAILD;
        }


        public HidDeviceData.HID_RETURN GetFeature(HidDeviceReport r)
        {
            if (deviceOpened)
            {
                try
                {
                    byte[] buffer = new byte[outputReportLength];
                    buffer[0] = r.reportID;
                    int maxBufferLength = 0;
                    if (r.reportBuff.Length < outputReportLength - 1)
                        maxBufferLength = r.reportBuff.Length;
                    else
                        maxBufferLength = outputReportLength - 1;

                    for (int i = 0; i < maxBufferLength; i++)
                        buffer[i + 1] = r.reportBuff[i];
                    bool isGetFeature = HidD_GetFeature(device, buffer, buffer.Length);

                    if (isGetFeature)
                    {                       
                        //Array.Copy(r.reportBuff, 0, buffer, 1, r.reportBuff.Length);
                        var str = BitConverter.ToString(buffer, 0).Replace("-", string.Empty).ToLower();
                        Console.WriteLine(str);
                        return HidDeviceData.HID_RETURN.SUCCESS;
                    }

                    return HidDeviceData.HID_RETURN.NO_DEVICE_CONECTED;
                }
                catch
                {
                    EventArgs ex = new EventArgs();
                    OnDeviceRemoved(ex);//发出设备移除消息
                    CloseDevice();
                    return HidDeviceData.HID_RETURN.NO_DEVICE_CONECTED;
                }
            }
            return HidDeviceData.HID_RETURN.WRITE_FAILD; ;
        }

        /// <summary>
        /// 获取所有连接的hid的设备路径
        /// </summary>
        /// <returns>包含每个设备路径的字符串数组</returns>
        public static void GetHidDeviceList(List<string> deviceList)
        {
            Guid hUSB = Guid.Empty;

            uint index = 0;

            deviceList.Clear();
            // 取得hid设备全局id
            HidD_GetHidGuid(ref hUSB);
            //取得一个包含所有HID接口信息集合的句柄
            IntPtr hidInfoSet = SetupDiGetClassDevs(ref hUSB, 0, IntPtr.Zero, DIGCF.DIGCF_PRESENT | DIGCF.DIGCF_DEVICEINTERFACE);
            if (hidInfoSet != IntPtr.Zero)
            {
                SP_DEVICE_INTERFACE_DATA interfaceInfo = new SP_DEVICE_INTERFACE_DATA();
                interfaceInfo.cbSize = Marshal.SizeOf(interfaceInfo);
                //查询集合中每一个接口
                for (index = 0; index < MAX_USB_DEVICES; index++)
                {
                    //得到第index个接口信息
                    if (SetupDiEnumDeviceInterfaces(hidInfoSet, IntPtr.Zero, ref hUSB, index, ref interfaceInfo))
                    {
                        int buffsize = 0;
                        // 取得接口详细信息:第一次读取错误,但可以取得信息缓冲区的大小
                        SetupDiGetDeviceInterfaceDetail(hidInfoSet, ref interfaceInfo, IntPtr.Zero, buffsize, ref buffsize, null);
                        //构建接收缓冲
                        IntPtr pDetail = Marshal.AllocHGlobal(buffsize);
                        SP_DEVICE_INTERFACE_DETAIL_DATA detail = new SP_DEVICE_INTERFACE_DETAIL_DATA();
                        detail.cbSize = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DETAIL_DATA));
                        Marshal.StructureToPtr(detail, pDetail, false);
                        if (SetupDiGetDeviceInterfaceDetail(hidInfoSet, ref interfaceInfo, pDetail, buffsize, ref buffsize, null))
                        {
                            deviceList.Add(Marshal.PtrToStringAuto((IntPtr)((int)pDetail + 4)));
                        }
                        Marshal.FreeHGlobal(pDetail);
                    }
                }
            }
            SetupDiDestroyDeviceInfoList(hidInfoSet);
            //return deviceList.ToArray();
        }


    }
}
