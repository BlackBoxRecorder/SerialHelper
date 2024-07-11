using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace SerialHelper
{
    public class SerialHex
    {
        private readonly SerialPort serial;
        private readonly ISerialHexLogger logger;

        private readonly Stopwatch swTimeout = Stopwatch.StartNew();
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);

        public SerialHex(ISerialHexLogger logger)
        {
            serial = new SerialPort();
            this.logger = logger;
        }

        #region Property
        public string PortName { get; set; }
        public int BaudRate { get; set; }
        public Parity Parity { get; set; }
        public int DataBits { get; set; }
        public StopBits StopBits { get; set; }
        public int ReadTimeout { get; set; } = 3000;
        public int WriteTimeout { get; set; } = 3000;

        /// <summary>
        /// 命令超时时间
        /// </summary>
        public int Timeout { get; set; } = 5000;
        public int ReadBufferSize { get; set; } = 8192;
        public int WriteBufferSize { get; set; } = 8192;

        #endregion

        #region Public

        public async Task<bool> Open()
        {
            try
            {
                await WaitLockAsync();

                serial.PortName = PortName;
                serial.BaudRate = BaudRate;
                serial.Parity = Parity;
                serial.DataBits = DataBits;
                serial.StopBits = StopBits;
                serial.ReadTimeout = ReadTimeout;
                serial.WriteTimeout = WriteTimeout;
                serial.ReadBufferSize = ReadBufferSize;
                serial.WriteBufferSize = WriteBufferSize;

                serial.ErrorReceived += Serial_ErrorReceived;

                serial.Open();

                logger.Info("串口已打开");

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, ex.Message);
            }
            finally
            {
                semaphore.Release();
            }

            return false;
        }

        public async Task Close()
        {
            try
            {
                await WaitLockAsync();
                serial.Close();
            }
            catch (Exception ex)
            {
                logger.Error(ex, ex.Message);
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task<byte[]> SendAsync(byte[] data, int recvLength)
        {
            try
            {
                await WaitLockAsync();
                return Send(data, recvLength);
            }
            catch (Exception ex)
            {
                logger.Error(ex, ex.Message);
                throw;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task<byte[]> ReceiveAsync(int recvLength)
        {
            try
            {
                await WaitLockAsync();
                return Receive(recvLength);
            }
            catch (Exception ex)
            {
                logger.Error(ex, ex.Message);
                throw;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public void ClearRecvBuffer()
        {
            try
            {
                serial.DiscardInBuffer();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "清空接收区缓存异常");
            }
        }

        public void ClearSendBuffer()
        {
            try
            {
                serial.DiscardOutBuffer();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "清空发送区缓存异常");
            }
        }

        #endregion

        #region Private

        private byte[] Send(byte[] data, int recvLength)
        {
            serial.Write(data, 0, data.Length);
            return Receive(recvLength);
        }

        private byte[] Receive(int recvLength)
        {
            var recv = new byte[recvLength];

            swTimeout.Restart();

            int totalReadBytes = 0;

            while (totalReadBytes < recvLength)
            {
                if (swTimeout.Elapsed.TotalMilliseconds > Timeout)
                {
                    throw new TimeoutException($"读取指定长度的数据超时, {Timeout}ms");
                }

                if (serial.BytesToRead < 1)
                {
                    continue;
                }

                var bytesRead = serial.Read(recv, totalReadBytes, recvLength - totalReadBytes);

                totalReadBytes += bytesRead;
            }

            return recv;
        }

        private async Task WaitLockAsync()
        {
            if (!await semaphore.WaitAsync(Timeout))
            {
                throw new TimeoutException("等待获取锁超时");
            }
        }

        private void Serial_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            SerialError error = e.EventType;
            logger.Info("SerialPort Error Received: " + error.ToString());
        }

        #endregion
    }

    public interface ISerialHexLogger
    {
        void Trace(string message);
        void Debug(string message);
        void Info(string message);
        void Error(Exception ex, string message);
    }

    public class SerialHexLogger : ISerialHexLogger
    {
        public void Error(Exception ex, string message)
        {
            Console.WriteLine($"{DateTime.Now} - {message}");
            Console.WriteLine($"{DateTime.Now} - {ex}");
        }

        public void Info(string message)
        {
            Console.WriteLine($"{DateTime.Now} - {message}");
        }

        public void Debug(string message)
        {
            Console.WriteLine($"{DateTime.Now} - {message}");
        }

        public void Trace(string message)
        {
            Console.WriteLine($"{DateTime.Now} - {message}");
        }
    }

    public static class Utils
    {
        public static string BytesToHexString(byte[] bytes)
        {
            return "0x" + string.Join(" 0x", Array.ConvertAll(bytes, b => b.ToString("X2")));
        }

        public static byte[] HexStringToBytes(string hexString, string separator)
        {
            hexString = hexString.Trim().Replace(separator, "");

            if (hexString.Length % 2 != 0)
            {
                throw new ArgumentException("HexString must have an even number of characters.");
            }

            byte[] byteArray = new byte[hexString.Length / 2];

            for (int i = 0; i < hexString.Length; i += 2)
            {
                byteArray[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }

            return byteArray;
        }
    }
}
