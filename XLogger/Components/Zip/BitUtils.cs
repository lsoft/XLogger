namespace XLogger.Components.Zip
{
    public class BitUtils
    {
        public static readonly int[] DEF_HUFFMAN_DYNTREE_CODELENGTHS_ORDER =
        {
            16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15
        };
        private static readonly byte[] DEF_REVERSE_BITS =
        {
            0, 8, 4, 12, 2, 10, 6, 14, 1, 9, 5, 13, 3, 11, 7, 15
        };
        public static short BitReverse(int value)
        {
            return (short)(DEF_REVERSE_BITS[value & 15] << 12
                           | DEF_REVERSE_BITS[(value >> 4) & 15] << 8
                           | DEF_REVERSE_BITS[(value >> 8) & 15] << 4
                           | DEF_REVERSE_BITS[value >> 12]);
        }
    }
}