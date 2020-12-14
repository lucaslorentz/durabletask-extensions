using System.Collections.Generic;
using System.Security.Cryptography;

namespace LLL.DurableTask.EFCore.Utils
{
    public static class Shuffler
    {
        private static RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                byte[] box = new byte[1];
                do rng.GetBytes(box);
                while (!(box[0] < n * (byte.MaxValue / n)));
                int k = (box[0] % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
