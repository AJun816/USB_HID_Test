using System;
using System.Threading;

namespace USB_HID_Test
{
    class Program
    {
        static void Main(string[] args)
        {
            HidDeviceSimple hidDevice = new HidDeviceSimple();
            //初始化自动连接
            hidDevice.Initial(0x0951, 0x16E4, "vid_0951&pid_16e4&mi_01&col05");

            //发送数据
            hidDevice.isConnectedFunc = new HidDeviceSimple.isConnectedDelegate(state =>
            {
                if (state)
                {
                    Console.WriteLine("============**********===========");
                    Console.WriteLine("连接成功");

                    byte[] bytes = new byte[] { 0x07, 0x83, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 };     
                    
                    bool isSend = hidDevice.ReadFeatureData(bytes);
                    Thread.Sleep(10);
                    bool isReceive = hidDevice.WriteFeatureData(7,bytes);

                    string str = System.Text.Encoding.Default.GetString(bytes);
                    Console.WriteLine($"发送结果：{isSend},发送的内容：{str}");
                    Console.WriteLine($"接收结果：{isReceive}，接收的内容{str}");
                    Console.WriteLine("================================");
                }
                else
                {
                    Console.WriteLine("连接失败");
                }
            });
            Console.Read();
        }
    }
}
