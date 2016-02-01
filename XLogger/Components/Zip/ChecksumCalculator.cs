using System;

namespace XLogger.Components.Zip
{

    public class ChecksumCalculator
    {
        private const int DEF_CHECKSUM_BASE = 65521;
        private const int DEF_CHECKSUM_BIT_OFFSET = 16;
        private const int DEF_CHECKSUM_ITERATIONSCOUNT = 3800;

        public static long ChecksumGenerate(byte[] buffer, int offset, int length)
        {
            long result = 1;
            ChecksumUpdate(ref result, buffer, offset, length);
            return result;
        }

        public static void ChecksumUpdate(ref long checksum, byte[] buffer, int offset, int length)
        {
            uint checksum_uint = (uint)checksum;
            uint s1 = checksum_uint & ushort.MaxValue;
            uint s2 = checksum_uint >> DEF_CHECKSUM_BIT_OFFSET;

            while (length > 0)
            {
                int steps = Math.Min(length, DEF_CHECKSUM_ITERATIONSCOUNT);
                length -= steps;

                while (--steps >= 0)
                {
                    s1 = s1 + (uint)(buffer[offset++] & byte.MaxValue);
                    s2 = s2 + s1;
                }

                s1 %= DEF_CHECKSUM_BASE;
                s2 %= DEF_CHECKSUM_BASE;
            }

            checksum_uint = (s2 << DEF_CHECKSUM_BIT_OFFSET) | s1;
            checksum = (long)checksum_uint;
        }
    }//class
    //class
  
}//ns
