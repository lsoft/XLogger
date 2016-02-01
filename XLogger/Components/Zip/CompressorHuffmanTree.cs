using System;
using System.Diagnostics;

namespace XLogger.Components.Zip
{

    public class CompressorHuffmanTree
    {

        private int m_CodeCount;
        private short[] m_CodeFrequences;
        private byte[] m_CodeLengths;

        private int m_CodeMinimumCount;
        private short[] m_Codes;
        private int[] m_LengthCounts;

        private int m_MaximumLength;

        private CompressedStreamWriter m_Writer;

        public short[] CodeFrequences
        {
            get
            {
                return m_CodeFrequences;
            }
        }

        public byte[] CodeLengths
        {
            get
            {
                return m_CodeLengths;
            }
        }

        public int TreeLength
        {
            get
            {
                return m_CodeCount;
            }
        }

        public CompressorHuffmanTree(CompressedStreamWriter writer, int iElementsCount,
                                      int iMinimumCodes, int iMaximumLength)
        {
            this.m_Writer = writer;
            this.m_CodeMinimumCount = iMinimumCodes;
            this.m_MaximumLength = iMaximumLength;
            m_CodeFrequences = new short[iElementsCount];
            m_LengthCounts = new int[iMaximumLength];
        }
        public void BuildCodes()
        {
            int[] nextCode = new int[m_MaximumLength];
            m_Codes = new short[m_CodeCount];
            int code = 0;

            for (int bitsCount = 0; bitsCount < m_MaximumLength; bitsCount++)
            {
                nextCode[bitsCount] = code;
                code += m_LengthCounts[bitsCount] << (15 - bitsCount);
            }

            for (int i = 0; i < m_CodeCount; i++)
            {
                int bits = m_CodeLengths[i];

                if (bits > 0)
                {
                    m_Codes[i] = BitUtils.BitReverse(nextCode[bits - 1]);
                    nextCode[bits - 1] += 1 << (16 - bits);
                }
            }
        }

        public void BuildTree()
        {
            int iCodesCount = m_CodeFrequences.Length;

            int[] arrTree = new int[iCodesCount];
            int iTreeLength = 0;
            int iMaxCode = 0;

            for (int n = 0; n < iCodesCount; n++)
            {
                int freq = m_CodeFrequences[n];

                if (freq != 0)
                {
                    int pos = iTreeLength++;
                    int ppos;

                    while (pos > 0 && m_CodeFrequences[arrTree[ppos = (pos - 1) / 2]] > freq)
                    {
                        arrTree[pos] = arrTree[ppos];
                        pos = ppos;
                    }

                    arrTree[pos] = n;

                    iMaxCode = n;
                }
            }

            while (iTreeLength < 2)
            {
                arrTree[iTreeLength++] =
                  (iMaxCode < 2) ? ++iMaxCode : 0;
            }

            m_CodeCount = Math.Max(iMaxCode + 1, m_CodeMinimumCount);

            int iLeafsCount = iTreeLength;
            int iNodesCount = iLeafsCount;
            int[] childs = new int[4 * iTreeLength - 2];
            int[] values = new int[2 * iTreeLength - 1];

            for (int i = 0; i < iTreeLength; i++)
            {
                int node = arrTree[i];
                int iIndex = 2 * i;
                childs[iIndex] = node;
                childs[iIndex + 1] = -1;
                values[i] = (m_CodeFrequences[node] << 8);
                arrTree[i] = i;
            }

            do
            {
                int first = arrTree[0];
                int last = arrTree[--iTreeLength];
                int lastVal = values[last];

                int ppos = 0;
                int path = 1;

                while (path < iTreeLength)
                {
                    if (path + 1 < iTreeLength &&
                      values[arrTree[path]] > values[arrTree[path + 1]])
                    {
                        path++;
                    }

                    arrTree[ppos] = arrTree[path];
                    ppos = path;
                    path = ppos * 2 + 1;
                }

                while ((path = ppos) > 0 && values[arrTree[ppos = (path - 1) / 2]] > lastVal)
                {
                    arrTree[path] = arrTree[ppos];
                }

                arrTree[path] = last;

                int second = arrTree[0];

                last = iNodesCount++;
                childs[2 * last] = first;
                childs[2 * last + 1] = second;
                int mindepth = Math.Min(values[first] & 0xff, values[second] & 0xff);
                values[last] =
                  lastVal =
                    values[first] + values[second] - mindepth + 1;

                ppos = 0;
                path = 1;

                while (path < iTreeLength)
                {
                    if (path + 1 < iTreeLength &&
                      values[arrTree[path]] > values[arrTree[path + 1]])
                    {
                        path++;
                    }

                    arrTree[ppos] = arrTree[path];
                    ppos = path;
                    path = ppos * 2 + 1;
                }

                while ((path = ppos) > 0 &&
                  values[arrTree[ppos = (path - 1) / 2]] > lastVal)
                {
                    arrTree[path] = arrTree[ppos];
                }

                arrTree[path] = last;
            }
            while (iTreeLength > 1);

            if (arrTree[0] != childs.Length / 2 - 1)
            {
                throw new ApplicationException("Heap invariant violated");
            }

            BuildLength(childs);
        }

        public void CalcBLFreq(CompressorHuffmanTree blTree)
        {
            int max_count;
            int min_count;
            int count;
            int curlen = -1;

            int i = 0;
            while (i < m_CodeCount)
            {
                count = 1;
                int nextlen = m_CodeLengths[i];
                if (nextlen == 0)
                {
                    max_count = 138;
                    min_count = 3;
                }
                else
                {
                    max_count = 6;
                    min_count = 3;
                    if (curlen != nextlen)
                    {
                        blTree.m_CodeFrequences[nextlen]++;
                        count = 0;
                    }
                }
                curlen = nextlen;
                i++;

                while (i < m_CodeCount && curlen == m_CodeLengths[i])
                {
                    i++;
                    if (++count >= max_count)
                    {
                        break;
                    }
                }

                if (count < min_count)
                {
                    blTree.m_CodeFrequences[curlen] += (short)count;
                }
                else if (curlen != 0)
                {
                    blTree.m_CodeFrequences[16]++;
                }
                else if (count <= 10)
                {
                    blTree.m_CodeFrequences[17]++;
                }
                else
                {
                    blTree.m_CodeFrequences[18]++;
                }
            }
        }
        public void CheckEmpty()
        {
            for (int i = 0; i < m_CodeFrequences.Length; i++)
            {
                Debug.Assert(m_CodeFrequences[i] == 0, "Huffman compressor error", "Huffman codes tree is not empty.");
            }
        }

        public int GetEncodedLength()
        {
            int len = 0;

            for (int i = 0; i < m_CodeFrequences.Length; i++)
            {
                len += m_CodeFrequences[i] * m_CodeLengths[i];
            }

            return len;
        }
        public void Reset()
        {
            for (int i = 0; i < m_CodeFrequences.Length; i++)
            {
                m_CodeFrequences[i] = 0;
            }

            m_Codes = null;
            m_CodeLengths = null;
        }
        public void SetStaticCodes(short[] codes, byte[] lengths)
        {
            m_Codes = (short[])codes.Clone();
            m_CodeLengths = (byte[])lengths.Clone();
        }
        public void WriteCodeToStream(int code)
        {
            m_Writer.PendingBufferWriteBits(m_Codes[code] & 0xffff, m_CodeLengths[code]);
        }

        public void WriteTree(CompressorHuffmanTree blTree)
        {
            int iMaxRepeatCount;
            int iMinRepeatCount;
            int iCurrentRepeatCount;
            int iCurrentCodeLength = -1;

            int i = 0;
            while (i < m_CodeCount)
            {
                iCurrentRepeatCount = 1;
                int nextlen = m_CodeLengths[i];

                if (nextlen == 0)
                {
                    iMaxRepeatCount = 138;
                    iMinRepeatCount = 3;
                }
                else
                {
                    iMaxRepeatCount = 6;
                    iMinRepeatCount = 3;

                    if (iCurrentCodeLength != nextlen)
                    {
                        blTree.WriteCodeToStream(nextlen);
                        iCurrentRepeatCount = 0;
                    }
                }

                iCurrentCodeLength = nextlen;
                i++;

                while (i < m_CodeCount && iCurrentCodeLength == m_CodeLengths[i])
                {
                    i++;

                    if (++iCurrentRepeatCount >= iMaxRepeatCount)
                    {
                        break;
                    }
                }

                if (iCurrentRepeatCount < iMinRepeatCount)
                {
                    while (iCurrentRepeatCount-- > 0)
                    {
                        blTree.WriteCodeToStream(iCurrentCodeLength);
                    }
                }
                else if (iCurrentCodeLength != 0)
                {
                    blTree.WriteCodeToStream(16);
                    m_Writer.PendingBufferWriteBits(iCurrentRepeatCount - 3, 2);
                }
                else if (iCurrentRepeatCount <= 10)
                {
                    blTree.WriteCodeToStream(17);
                    m_Writer.PendingBufferWriteBits(iCurrentRepeatCount - 3, 3);
                }
                else
                {
                    blTree.WriteCodeToStream(18);
                    m_Writer.PendingBufferWriteBits(iCurrentRepeatCount - 11, 7);
                }
            }
        }

        private void BuildLength(int[] childs)
        {
            m_CodeLengths = new byte[m_CodeFrequences.Length];
            int numNodes = childs.Length / 2;
            int numLeafs = (numNodes + 1) / 2;
            int overflow = 0;

            for (int i = 0; i < m_MaximumLength; i++)
            {
                m_LengthCounts[i] = 0;
            }

            int[] lengths = new int[numNodes];
            lengths[numNodes - 1] = 0;

            for (int i = numNodes - 1; i >= 0; i--)
            {
                int iChildIndex = 2 * i + 1;

                if (childs[iChildIndex] != -1)
                {
                    int bitLength = lengths[i] + 1;

                    if (bitLength > m_MaximumLength)
                    {
                        bitLength = m_MaximumLength;
                        overflow++;
                    }

                    lengths[childs[iChildIndex - 1]] = lengths[childs[iChildIndex]] = bitLength;
                }
                else
                {
                    int bitLength = lengths[i];
                    m_LengthCounts[bitLength - 1]++;
                    m_CodeLengths[childs[iChildIndex - 1]] = (byte)lengths[i];
                }
            }

            if (overflow == 0)
            {
                return;
            }

            int iIncreasableLength = m_MaximumLength - 1;

            do
            {
                while (m_LengthCounts[--iIncreasableLength] == 0)
                {
                    ;
                }

                do
                {
                    m_LengthCounts[iIncreasableLength]--;
                    m_LengthCounts[++iIncreasableLength]++;
                    overflow -= (1 << (m_MaximumLength - 1 - iIncreasableLength));
                }
                while (overflow > 0 && iIncreasableLength < m_MaximumLength - 1);
            }
            while (overflow > 0);

            m_LengthCounts[m_MaximumLength - 1] += overflow;
            m_LengthCounts[m_MaximumLength - 2] -= overflow;

            int nodePtr = 2 * numLeafs;

            for (int bits = m_MaximumLength; bits != 0; bits--)
            {
                int n = m_LengthCounts[bits - 1];

                while (n > 0)
                {
                    int childPtr = 2 * childs[nodePtr++];

                    if (childs[childPtr + 1] == -1)
                    {
                        m_CodeLengths[childs[childPtr]] = (byte)bits;
                        n--;
                    }
                }
            }
        }
    }//class
    //class

}//ns
