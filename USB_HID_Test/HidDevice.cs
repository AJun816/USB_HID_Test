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
        private readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        private const int MAX_USB_DEVICES = 64;
        private bool deviceOpened = false;
        private readonly FileStream hidDevice = null;
        private IntPtr hHubDevice;

        int outputReportLength;//输出报告长度,包刮一个字节的报告ID
        public int OutputReportLength { get { return outputReportLength; } }
        int inputReportLength;//输入报告长度,包刮一个字节的报告ID   
        public int InputReportLength { get { return inputReportLength; } }
        short featureReportByteLength;
        public short FeatureReportByteLength { get { return featureReportByteLength; } }
        IntPtr device = IntPtr.Zero;

        public enum DeviceMode
        {
            NonOverlapped = 0,
            Overlapped = 1
        }

        [Flags]
        public enum ShareMode
        {
            Exclusive = 0,
            ShareRead = NativeMethods.FILE_SHARE_READ,
            ShareWrite = NativeMethods.FILE_SHARE_WRITE
        }

        private static IntPtr OpenDeviceIO(string devicePath, DeviceMode deviceMode, uint deviceAccess, ShareMode shareMode)
        {
            var security = new NativeMethods.SECURITY_ATTRIBUTES();
            var flags = 0;

            if (deviceMode == DeviceMode.Overlapped) flags = NativeMethods.FILE_FLAG_OVERLAPPED;

            security.lpSecurityDescriptor = IntPtr.Zero;
            security.bInheritHandle = true;
            security.nLength = Marshal.SizeOf(security);
            return NativeMethods.CreateFile(devicePath, deviceAccess, (int)shareMode, ref security, NativeMethods.OPEN_EXISTING, flags, hTemplateFile: IntPtr.Zero);
        }


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
                //连接的HID列表
                List<string> deviceList = new List<string>();
                //获取连接的HID列表
                GetHidDeviceList(deviceList);

                if (deviceList.Count == 0)
                    return HidDeviceData.HID_RETURN.NO_DEVICE_CONECTED;
                for (int i = 0; i < deviceList.Count; i++)
                {
                    //找到指定的HID设备
                    if (deviceList[i].IndexOf("vid_0951&pid_16e4&mi_01&col05") > -1)
                    {    
                        //05 打开HID设备获得设备句柄
                        device = OpenDeviceIO(deviceList[i], DeviceMode.Overlapped, NativeMethods.GENERIC_WRITE, ShareMode.ShareRead | ShareMode.ShareWrite);
               
                        if (device != INVALID_HANDLE_VALUE)
                        {
                            IntPtr serialBuff = Marshal.AllocHGlobal(512);
                            //06 填写HIDD_ATTRIBUTES结构的数据项，该结构包含设备的厂商ID、产品ID和产品序列号，比照这些数值确定该设备是否是查找的设备
                            HidD_GetAttributes(device, out HIDD_ATTRIBUTES attributes);
                            HidD_GetSerialNumberString(device, serialBuff, 512);
                            string deviceStr = Marshal.PtrToStringAuto(serialBuff);
                            Marshal.FreeHGlobal(serialBuff);
                            if (attributes.VendorID == vID && attributes.ProductID == pID)
                            {
                                IntPtr preparseData;                              
                                var capabilities = default(NativeMethods.HIDP_CAPS);
                                //07 请求获得与设备能力信息相关的缓冲区的代号
                                HidD_GetPreparsedData(device, out preparseData);
                                //08 获取HID能力值，通过能力值判断是否是需要寻找的设备
                                HidP_GetCaps(preparseData, ref capabilities);
                                HidD_FreePreparsedData(preparseData);
                                outputReportLength = capabilities.OutputReportByteLength;
                                inputReportLength = capabilities.InputReportByteLength;
                                featureReportByteLength = capabilities.FeatureReportByteLength;
                                deviceOpened = true;
                                hHubDevice = device;
                                return HidDeviceData.HID_RETURN.SUCCESS;
                            }
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

        private static byte[] CreateBuffer(int length)
        {
            byte[] buffer = null;
            Array.Resize(ref buffer, length + 1);
            return buffer;
        }

        public HidDeviceData.HID_RETURN SetFeature(HidDeviceReport r)
        {
            if (deviceOpened)
            {
                try
                {
                    byte[] buffer = CreateBuffer(FeatureReportByteLength-1);
                    buffer[0] = r.reportID;
                    Array.Copy(r.reportBuff, 0, buffer, 0, r.reportBuff.Length);
                    bool isSetFeature = HidD_SetFeature(device, buffer, buffer.Length);
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
                    byte[] buffer = CreateBuffer(FeatureReportByteLength - 1);
                    buffer[0] = r.reportID;

                    bool isGetFeature = HidD_GetFeature(device, buffer, buffer.Length);
                    if (isGetFeature)
                    {                       
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
            //01 取得hid设备全局id
            HidD_GetHidGuid(ref hUSB);
            //02 取得一个包含所有HID接口信息集合的句柄
            IntPtr hidInfoSet = SetupDiGetClassDevs(ref hUSB, 0, IntPtr.Zero, DIGCF.DIGCF_PRESENT | DIGCF.DIGCF_DEVICEINTERFACE);
            if (hidInfoSet != IntPtr.Zero)
            {
                SP_DEVICE_INTERFACE_DATA interfaceInfo = new SP_DEVICE_INTERFACE_DATA();
                interfaceInfo.cbSize = Marshal.SizeOf(interfaceInfo);
                //查询集合中每一个接口
                for (index = 0; index < MAX_USB_DEVICES; index++)
                {
                    //03 得到第index个接口信息，该结构用于识别一个HID设备接口
                    if (SetupDiEnumDeviceInterfaces(hidInfoSet, IntPtr.Zero, ref hUSB, index, ref interfaceInfo))
                    {
                        int buffsize = 0;
                        //04 取得接口详细信息:第一次读取错误,但可以取得信息缓冲区的大小，获得一个指向该设备的路径名
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
