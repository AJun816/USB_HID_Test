using System;

namespace USB_HID_Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Equipment equipment = new Equipment();
            //初始化自动连接
            equipment.Initial();
            //发送数据
            equipment.isConnectedFunc = new Equipment.isConnectedDelegate(state =>
            {
                if (state)
                {
                    Console.WriteLine("============**********===========");
                    Console.WriteLine("连接成功");
                    byte[] bytes = new byte[] { 0x07, 0x15, 0x02, 0x01, 0x00, 0x00, 0x00, 0x00 };
                    string str = System.Text.Encoding.Default.GetString(bytes);
                    bool isSend = equipment.SendBytes(bytes);

                    bool isReceive = equipment.ReceiveBytes(bytes);
                    Console.WriteLine($"发送结果：{isSend},发送的内容：{str}");
                    Console.WriteLine($"接收结果：{isReceive}，发送内容{str}");
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
