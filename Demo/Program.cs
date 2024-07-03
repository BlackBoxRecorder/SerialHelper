using System.IO.Ports;
using SerialHelper;

Console.WriteLine("Hello, World!");

var ports = SerialPort.GetPortNames();

foreach (var port in ports)
{
    Console.WriteLine(port);
}

SerialHex serial = new SerialHex(new SerialHexLogger());

serial.PortName = "COM1";
serial.BaudRate = 9600;
serial.DataBits = 8;
serial.Parity = System.IO.Ports.Parity.None;
serial.StopBits = System.IO.Ports.StopBits.One;
serial.Timeout = 5000;

bool isOpened = await serial.Open();
if (!isOpened)
{
    Console.WriteLine("串口未打开");
    return;
}

int idx = 0;
while (true)
{
    var data = new byte[4] { 0x01, 0x02, 0x03, 0x04 };
    var recv = await serial.SendAsync(data, 8);

    Console.WriteLine($"{DateTime.Now} {idx.ToString().PadLeft(4, '0')} 收到数据长度：{recv.Length}");

    await Task.Delay(10);

    idx++;

    if (idx > 1000)
    {
        break;
    }
}
Console.WriteLine("END");
Console.ReadLine();
