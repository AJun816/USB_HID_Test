using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace USB_HID_Test
{
    public class NativeMethods
    {
        internal const short FILE_SHARE_READ = 0x1;
        internal const short FILE_SHARE_WRITE = 0x2;
        internal const int FILE_FLAG_OVERLAPPED = 0x40000000;
        internal const short OPEN_EXISTING = 3;
        internal const uint GENERIC_READ = 0x80000000;
        internal const uint GENERIC_WRITE = 0x40000000;


        #region Win32API
        /// <summary>
        /// HID D获取HID GUID例程返回HIDClass设备的设备接口GUID。
        /// </summary>
        /// <param name="HidGuid">调用者分配的GUID缓冲区，其用于返回HIDClass设备的设备界面GUID.</param>
        [DllImport("hid.dll")]
        static internal extern void HidD_GetHidGuid(ref Guid HidGuid);

        [DllImport("hid.dll")]
        static internal extern bool HidD_SetFeature(IntPtr hidDeviceObject, byte[] lpReportBuffer, int reportBufferLength);

        [DllImport("hid.dll", SetLastError = true)]
        static internal extern bool HidD_GetFeature(IntPtr hidDeviceObject, byte[] lpReportBuffer, int reportBufferLength);

        /// <summary>
        /// 设置DI GET类DEVS功能返回给包含本地计算机所请求的设备信息元素的设备信息集的句柄。 
        /// </summary>
        /// <param name="ClassGuid">GUID for Device Setup类或设备接口类。</param>
        /// <param name="Enumerator">指向空端接终止字符串的指针，用于提供PN P枚举器的名称或PN P设备实例标识符。 </param>
        /// <param name="HwndParent">用于用户界面的顶级窗口的句柄</param>
        /// <param name="Flags">一个可变的变量，指定过滤添加到设备信息集的设备信息元素的控制选项。 </param>
        /// <returns>设备信息集的句柄 </returns>
        [DllImport("setupapi.dll", SetLastError = true)]
        static internal extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, uint Enumerator, IntPtr HwndParent, DIGCF Flags);

        /// <summary>
        /// Setup DI Destroy设备信息列表功能删除设备信息集并释放所有相关内存。
        /// </summary>
        /// <param name="DeviceInfoSet">设备信息设置为删除的句柄。</param>
        /// <returns>如果成功返回true。否则，它返回错误 </returns>
        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static internal extern Boolean SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

        /// <summary>
        /// Setup DI Enum设备接口功能枚举设备信息集中包含的设备接口。
        /// </summary>
        /// <param name="deviceInfoSet">指向设备信息集的指针，该设备包含用于返回信息的设备接口</param>
        /// <param name="deviceInfoData">指向SP DevInfo数据结构的指针，该数据结构指定设备信息集中的设备信息元素</param>
        /// <param name="interfaceClassGuid">指定所请求接口的设备接口类的GUID</param>
        /// <param name="memberIndex">基于零索引到设备信息集中的接口列表中</param>
        /// <param name="deviceInterfaceData">一个调用者分配的缓冲区，包含一个已完成的SP设备接口数据结构，其标识符合搜索参数的接口</param>
        /// <returns></returns>
        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static internal extern Boolean SetupDiEnumDeviceInterfaces(IntPtr deviceInfoSet, IntPtr deviceInfoData, ref Guid interfaceClassGuid, UInt32 memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        /// <summary>
        /// Setup DI Get Device接口详细信息功能返回有关设备界面的详细信息。
        /// </summary>
        /// <param name="deviceInfoSet">指向设备信息集的指针，该设备包含用于检索详细信息的接口</param>
        /// <param name="deviceInterfaceData">指向SP设备接口数据结构的指针，该数据结构指定用于检索详细信息的设备信息集中的接口</param>
        /// <param name="deviceInterfaceDetailData">指向SP设备接口详细数据结构的指针，以接收有关指定接口的信息</param>
        /// <param name="deviceInterfaceDetailDataSize">设备接口细节数据缓冲区的大小</param>
        /// <param name="requiredSize">指向变量的指针，该变量接收设备接口详细数据缓冲区所需的大小</param>
        /// <param name="deviceInfoData">指针缓冲区，用于接收有关支持所请求界面的设备的信息</param>
        /// <returns></returns>
        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static internal extern bool SetupDiGetDeviceInterfaceDetail(IntPtr deviceInfoSet, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, IntPtr deviceInterfaceDetailData, int deviceInterfaceDetailDataSize, ref int requiredSize, SP_DEVINFO_DATA deviceInfoData);

        /// <summary>
        /// HID D Get属性例程返回指定的顶级集合的属性。
        /// </summary>
        /// <param name="HidDeviceObject">指定顶级集合的打开句柄</param>
        /// <param name="Attributes">调用者分配的HIDD属性结构，返回HID设备对象指定的集合的属性</param>
        /// <returns></returns>
        [DllImport("hid.dll")]
        static internal extern Boolean HidD_GetAttributes(IntPtr hidDeviceObject, out HIDD_ATTRIBUTES attributes);
        /// <summary>
        ///HID D获取序列号字符串例程返回顶级集合的嵌入式字符串，该集合标识集合物理设备的序列号.
        /// </summary>
        /// <param name="HidDeviceObject">指定顶级集合的打开句柄</param>
        /// <param name="Buffer">呼叫者分配的缓冲区，其例程用于返回所请求的序列号字符串</param>
        /// <param name="BufferLength">指定在缓冲区中提供的呼叫者分配缓冲区的长度（以字节为单位）</param>
        /// <returns></returns>
        [DllImport("hid.dll")]
        static internal extern Boolean HidD_GetSerialNumberString(IntPtr hidDeviceObject, IntPtr buffer, int bufferLength);

        /// <summary>
        /// HID D获得准备的数据例程返回顶级集合的准备数据。
        /// </summary>
        /// <param name="hidDeviceObject">指定顶级集合的打开句柄。 </param>
        /// <param name="PreparsedData">指向一个例程分配缓冲区的地址，该缓冲区包含一个集合的HIDP准备数据结构中的准备数据。</ param>
        /// <returns> HID D如果成功，则获得准备数据返回true;否则，它返回false</returns>
        [DllImport("hid.dll")]
        static internal extern Boolean HidD_GetPreparsedData(IntPtr hidDeviceObject, out IntPtr PreparsedData);       
        
        /// <summary>
        /// 此函数关闭开放对象句柄。
        /// </summary>
        /// <param name="hObject">掌握开放对象</param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        static internal extern int CloseHandle(IntPtr hObject);

        /// <summary>
        /// 此函数从文件中读取文件，从文件指针指示的位置开始。
        /// </summary>
        /// <param name="file">处理要读取的文件</param>
        /// <param name="buffer">指向从文件读取的数据的缓冲区 </param>
        /// <param name="numberOfBytesToRead">要从文件中读取的字节数</param>
        /// <param name="numberOfBytesRead">指向读取的字节数</param>
        /// <param name="lpOverlapped">不支持;设置为null.</param>
        /// <returns></returns>
        [DllImport("Kernel32.dll", SetLastError = true)]
        static internal extern bool ReadFile(IntPtr file, byte[] buffer, uint numberOfBytesToRead, out uint numberOfBytesRead, IntPtr lpOverlapped);

        /// <summary>
        ///  此功能将数据写入文件
        /// </summary>
        /// <param name="file">处理要写入的文件</param>
        /// <param name="buffer">指向包含要写入文件的数据的缓冲区</param>
        /// <param name="numberOfBytesToWrite">写入文件的字节数</param>
        /// <param name="numberOfBytesWritten">指向此函数调用的字节数的指针</param>
        /// <param name="lpOverlapped">不支持;设置为null.</param>
        /// <returns></returns>
        [DllImport("Kernel32.dll", SetLastError = true)]
        static internal extern bool WriteFile(IntPtr file, byte[] buffer, uint numberOfBytesToWrite, out uint numberOfBytesWritten, IntPtr lpOverlapped);

        /// <summary>
        /// 注册设备或窗口将接收通知的设备类型或类型
        /// </summary>
        /// <param name="recipient">窗口或服务的句柄将接收通知滤波器参数中指定的设备的设备事件</param>
        /// <param name="notificationFilter">指向数据块的指针，该数据指定应发送通知的设备类型</param>
        /// <param name="flags">指定句柄类型的标志</param>
        /// <returns>如果函数成功，则返回值是设备通知句柄</returns>
        [DllImport("User32.dll", SetLastError = true)]
        static internal extern IntPtr RegisterDeviceNotification(IntPtr recipient, IntPtr notificationFilter, int flags);

        /// <summary>
        /// 关闭指定的设备通知句柄。
        /// </summary>
        /// <param name="handle">寄存器设备通知功能返回的设备通知句柄</param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        static internal extern bool UnregisterDeviceNotification(IntPtr handle);

        [DllImport("hid.dll")]
        static internal extern bool HidD_GetInputReport(IntPtr hidDeviceObject, byte[] lpReportBuffer, int reportBufferLength);

        [DllImport("hid.dll")]
        static internal extern Boolean HidD_FreePreparsedData(IntPtr PreparsedData);

        [DllImport("hid.dll")]
        static internal extern int HidP_GetCaps(IntPtr preparsedData, ref HIDP_CAPS capabilities);

        [DllImport("kernel32.dll", SetLastError = true)]
        static internal extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, int dwShareMode, ref SECURITY_ATTRIBUTES lpSecurityAttributes, int dwCreationDisposition, int dwFlagsAndAttributes, IntPtr hTemplateFile);
        #endregion

        [StructLayout(LayoutKind.Sequential)]
        internal struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        /// <summary>
        /// SP设备接口数据结构定义了设备信息集中的设备接口。
        /// </summary>
        internal struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid interfaceClassGuid;//16
            public int flags;
            public int reserved;
        }

        /// <summary>
        /// SP设备接口详细数据结构包含设备接口的路径。
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        internal struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            internal int cbSize;
            internal short devicePath;
        }

        /// <summary>
        /// SP DevInfo数据结构定义了作为设备信息集成员的设备实例。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal class SP_DEVINFO_DATA
        {
            public int cbSize = Marshal.SizeOf(typeof(SP_DEVINFO_DATA));
            public Guid classGuid = Guid.Empty; // temp
            public int devInst = 0; // dumy
            public int reserved = 0;
        }
        /// <summary>
        /// 控制由Setup di Get Class Devs构建的设备信息集中包含的内容
        /// </summary>
        internal enum DIGCF
        {
            DIGCF_DEFAULT = 0x00000001, // only valid with DIGCF_DEVICEINTERFACE                 
            DIGCF_PRESENT = 0x00000002,
            DIGCF_ALLCLASSES = 0x00000004,
            DIGCF_PROFILE = 0x00000008,
            DIGCF_DEVICEINTERFACE = 0x00000010
        }
        /// <summary>
        /// HIDD属性结构包含有关HIDClass Devic的供应商信息e
        /// </summary>
        internal struct HIDD_ATTRIBUTES
        {
            public int Size;
            public ushort VendorID;
            public ushort ProductID;
            public ushort VersionNumber;
        }

        /// <summary>
        /// HID能力值信息
        /// </summary>
        internal struct HIDP_CAPS
        {
            internal short Usage;
            internal short UsagePage;
            internal short InputReportByteLength;
            internal short OutputReportByteLength;
            internal short FeatureReportByteLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            internal short[] Reserved;
            internal short NumberLinkCollectionNodes;
            internal short NumberInputButtonCaps;
            internal short NumberInputValueCaps;
            internal short NumberInputDataIndices;
            internal short NumberOutputButtonCaps;
            internal short NumberOutputValueCaps;
            internal short NumberOutputDataIndices;
            internal short NumberFeatureButtonCaps;
            internal short NumberFeatureValueCaps;
            internal short NumberFeatureDataIndices;
        }
        /// <summary>
        /// Type of access to the object. 
        ///</summary>
        static internal class DESIREDACCESS
        {
            public const uint GENERIC_READ = 0x80000000;
            public const uint GENERIC_WRITE = 0x40000000;
            public const uint GENERIC_EXECUTE = 0x20000000;
            public const uint GENERIC_ALL = 0x10000000;
        }
        /// <summary>
        /// 采取存在的文件，存在文件，并且在文件不存在时采取的操作。
        /// </summary>
        static internal class CREATIONDISPOSITION
        {
            public const uint CREATE_NEW = 1;
            public const uint CREATE_ALWAYS = 2;
            public const uint OPEN_EXISTING = 3;
            public const uint OPEN_ALWAYS = 4;
            public const uint TRUNCATE_EXISTING = 5;
        }
        /// <summary>
        /// 文件属性和文件的标志。
        /// </summary>
        static internal class FLAGSANDATTRIBUTES
        {
            public const uint FILE_FLAG_WRITE_THROUGH = 0x80000000;
            public const uint FILE_FLAG_OVERLAPPED = 0x40000000;
            public const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
            public const uint FILE_FLAG_RANDOM_ACCESS = 0x10000000;
            public const uint FILE_FLAG_SEQUENTIAL_SCAN = 0x08000000;
            public const uint FILE_FLAG_DELETE_ON_CLOSE = 0x04000000;
            public const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
            public const uint FILE_FLAG_POSIX_SEMANTICS = 0x01000000;
            public const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;
            public const uint FILE_FLAG_OPEN_NO_RECALL = 0x00100000;
            public const uint FILE_FLAG_FIRST_PIPE_INSTANCE = 0x00080000;
        }
        /// <summary>
        /// 用作标准标题，用于与通过WM DeviceChange消息报告的设备事件相关的信息。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct DEV_BROADCAST_HDR
        {
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;
        }
        /// <summary>
        ///包含有关一类设备的信息
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct DEV_BROADCAST_DEVICEINTERFACE
        {
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;
            public Guid dbcc_classguid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string dbcc_name;
        }
       
    }
}
