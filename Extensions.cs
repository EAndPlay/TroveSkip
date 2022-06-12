using System;

namespace TroveSkip
{
    public static class Extensions
    {
        /// <summary>
        /// Записывает все элементы массива <paramref name="from"/> в выходной массив, после элементов <paramref name="to"/>.
        /// </summary>
        /// <param name="to"></param>
        /// <param name="from"></param>
        public static int[] Join(this int[] to, int[] from)
        {
            var output = (int[])to.Clone();
            Array.Resize(ref output, from.Length + to.Length);
            from.CopyTo(output, to.Length);
            return output;
        }
        public static int[] Join(this int[] to, int from) => to.Join(new[] { from });
        /// <summary>
        /// Записывает все элементы массива <paramref name="from"/> в выходной массив, после элементов <paramref name="to"/>.
        /// </summary>
        /// <param name="to"></param>
        /// <param name="from"></param>
        public static byte[] Join(this byte[] to, byte[] from)
        {
            var output = (byte[])to.Clone();
            Array.Resize(ref output, from.Length + to.Length);
            from.CopyTo(output, to.Length);
            return output;
        }
        public static byte[] Join(this byte[] to, byte from) => to.Join(new[] { from });

        public static string ToHex(this long val) => Convert.ToString(val, 16);

        public static string BytesToString(this byte[] bytes)
        {
            var str = string.Empty;
            foreach (var b in bytes)
            {
                str += b.ToString("X") + ' ';
            }

            return str;
        }
        public static string IntsToString(this int[] ints)
        {
            var str = string.Empty;
            foreach (var b in ints)
            {
                str += b.ToString("X") + ' ';
            }

            return str;
        }
    }
}
