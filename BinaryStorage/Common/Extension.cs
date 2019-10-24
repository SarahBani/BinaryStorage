using System.Linq;

namespace BinStorage.Common
{
    public static class Extension
    {

        /// <summary>
        /// Used for comparing 2 byte arrays
        /// we can also use SequenceEqual where is needed, but I prefer using this method
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static bool IsEqual(this byte[] first, byte[] second)
        {
            return first.SequenceEqual(second);
        }

    }
}
