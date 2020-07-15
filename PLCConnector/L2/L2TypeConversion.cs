using Sharp7;
using System;
using System.Collections.Generic;
using System.Text;

namespace PLCConnector.L2
{
    public static class L2TypeConversion
    {

        static readonly DateTime BARMAG_START_DATE = new DateTime(1992, 1, 1);


        public static string AsSiemensChars(this DataField field)
        {
            var buffer = field.AsSiemensByteArray();
            return S7.GetCharsAt(buffer, 0, 4);
        }

        public static byte[] AsSiemensByteArray(this DataField field)
        {
            var buffer = new byte[4];
            S7.SetDIntAt(buffer, 0, field.As<int>());
            return buffer;
        }

        public static DateTime AsBarmagDate(this int num_days)
        {
            return BARMAG_START_DATE.AddDays(num_days);
        }

        public static TimeSpan AsBarmagTime(this int num_ms)
        {
            return TimeSpan.FromMilliseconds(num_ms);
        }

        public static bool[] AsBitMap(this DataField field)
        {
            var buffer = field.AsSiemensByteArray();
            var result = new bool[32];
            Array.Copy(ByteToBitMap(buffer[0]), 0, result, 0, 8);
            Array.Copy(ByteToBitMap(buffer[1]), 0, result, 8, 8);
            Array.Copy(ByteToBitMap(buffer[2]), 0, result, 16, 8);
            Array.Copy(ByteToBitMap(buffer[3]), 0, result, 24, 8);

            return result;
        }

        static bool[] ByteToBitMap(byte b)
        {
            var result = new bool[8];

            for (int i = 0; i < 8; i++)
                result[i] = (b & (1 << i)) > 0;

            return result;
        }

    }
}
