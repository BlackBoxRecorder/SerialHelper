using System;
using System.Collections.Generic;
using System.Text;

namespace SerialHelper
{
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
