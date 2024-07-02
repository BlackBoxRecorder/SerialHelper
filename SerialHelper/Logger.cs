using System;
using System.Collections.Generic;
using System.Text;

namespace SerialHelper
{
    public interface ISerialHexLogger
    {
        void LogTrace(string message);
        void LogDebug(string message);
        void LogInfo(string message);
        void LogError(string message, Exception ex);
    }

    public class SerialHexLogger : ISerialHexLogger
    {
        public void LogError(string message, Exception ex)
        {
            Console.WriteLine($"{DateTime.Now} - {message}");
            Console.WriteLine($"{DateTime.Now} - {ex}");
        }

        public void LogInfo(string message)
        {
            Console.WriteLine($"{DateTime.Now} - {message}");
        }

        public void LogDebug(string message)
        {
            Console.WriteLine($"{DateTime.Now} - {message}");
        }

        public void LogTrace(string message)
        {
            Console.WriteLine($"{DateTime.Now} - {message}");
        }
    }
}
