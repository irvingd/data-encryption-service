using System;
using System.Text;

namespace DataEncryptionService
{
    public static class Extensions
    {
        static public string ToBase64(this string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
            }

            return value;
        }

        static public string ToBase64(this byte[] byteArray)
        {
            if (byteArray?.Length > 0)
            {
                return Convert.ToBase64String(byteArray);
            }

            return null;
        }

        static public string ToByteSequence(this byte[] byteArray)
        {
            if (byteArray?.Length > 0)
            {
                StringBuilder sb = new StringBuilder(byteArray.Length * 2);
                foreach (var element in byteArray)
                {
                    sb.AppendFormat("{0:x2}", element);
                }
                return sb.ToString();
            }

            return string.Empty;
        }
    }
}
