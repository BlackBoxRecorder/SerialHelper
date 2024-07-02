using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Net.Sockets;
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
        public int ReadTimeout { get; set; }
        public int WriteTimeout { get; set; }
        public int Timeout { get; set; }
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
                serial.ReadTimeout = 2000;
                serial.WriteTimeout = 1000;

                serial.ErrorReceived += Serial_ErrorReceived;

                serial.Open();

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message, ex);
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
                logger.LogError(ex.Message, ex);
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
                logger.LogError(ex.Message, ex);
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
                logger.LogError(ex.Message, ex);
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
                logger.LogError("清空接收区缓存异常", ex);
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
                logger.LogError("清空发送区缓存异常", ex);
            }
        }

        #endregion

        #region Private

        public byte[] Send(byte[] data, int recvLength)
        {
            serial.Write(data, 0, data.Length);
            return Receive(recvLength);
        }

        public byte[] Receive(int recvLength)
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
            logger.LogInfo("SerialPort Error Received: " + error.ToString());
        }

        #endregion
    }
}
