using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarshalUtil
{
    public static class StringExtender
    {
        // Based on https://msdn.microsoft.com/en-us/library/bb311038.aspx
        public static string FromHex(this string str)
        {
            List<string> pairs = new List<string>();
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < str.Length - 3; i += 2)
                pairs.Add(str.Substring(i, 2));

            foreach (string strVal in pairs.Select(s => Convert.ToInt32(s, 16)).Select(char.ConvertFromUtf32))
            {
                sb.Append(strVal);
            }

            return sb.ToString();
        }

        public static string ToHex(this string str)
        {
            StringBuilder sb = new StringBuilder();

            foreach (string hex in from int charCode in str select Convert.ToString(charCode, 16) into hex select hex.PadLeft(2, '0'))
            {
                sb.Append(hex);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Converts from Little Endian to Big Endian
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static string ToBigEndian(this string hex)
        {
            if (hex.Length % 2 == 1)
                return hex;

            List<string> pairs = new List<string>();
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < hex.Length - 1; i += 2)
                pairs.Add(hex.Substring(i, 2));

            pairs.Reverse();

            foreach (string s in pairs)
                sb.Append(s);

            return sb.ToString();
        }

    }
}
