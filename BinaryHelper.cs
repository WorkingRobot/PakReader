using System.Linq;

namespace PakReader
{
    static class BinaryHelper
    {
        public static uint Flip(uint value) => (value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 |
         (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24;

        static readonly uint[] _Lookup32 = Enumerable.Range(0, 256).Select(i => {
            string s = i.ToString("x2");
            return s[0] + ((uint)s[1] << 16);
        }).ToArray();
        public static string ToHex(byte[] bytes)
        {
            if (bytes == null) return null;
            var length = bytes.Length;
            var result = new char[length * 2];
            for (int i = 0; i < length; i++)
            {
                var val = _Lookup32[bytes[i]];
                result[2 * i] = (char)val;
                result[2 * i + 1] = (char)(val >> 16);
            }
            return new string(result);
        }
    }
}
