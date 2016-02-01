using System;
using System.Diagnostics;
using System.IO;

namespace XLogger.Components.Zip
{
    public class CompressedStreamWriter
    {

        public const int HASH_BITS = DEFAULT_MEM_LEVEL + 7;

        public const int HASH_MASK = HASH_SIZE - 1;

        public const int HASH_SHIFT = (HASH_BITS + MIN_MATCH - 1) / MIN_MATCH;

        public const int HASH_SIZE = 1 << HASH_BITS;

        public const int MAX_DIST = WSIZE - MIN_LOOKAHEAD;

        public const int MAX_MATCH = 258;

        public const int MIN_LOOKAHEAD = MAX_MATCH + MIN_MATCH + 1;

        public const int MIN_MATCH = 3;

        public const int WMASK = WSIZE - 1;

        private const int DEF_HUFFMAN_BITLEN_TREE_LENGTH = 19;

        private const int DEF_HUFFMAN_BUFFER_SIZE = 1 << (DEFAULT_MEM_LEVEL + 6);

        private const int DEF_HUFFMAN_DISTANCES_ALPHABET_LENGTH = 30;

        private const int DEF_HUFFMAN_ENDBLOCK_SYMBOL = 256;

        private const int DEF_HUFFMAN_LITERAL_ALPHABET_LENGTH = 286;

        private const int DEF_PENDING_BUFFER_SIZE = 1 << (DEFAULT_MEM_LEVEL + 8);

        private const int DEF_ZLIB_HEADER_TEMPLATE = (8 + (7 << 4)) << 8;

        private const int DEFAULT_MEM_LEVEL = 8;

        private const int TOO_FAR = 4096;

        private const int WSIZE = 1 << 15;

        public static int[] COMPR_FUNC = { 0, 1, 1, 1, 1, 2, 2, 2, 2, 2 };

        public static int[] GOOD_LENGTH = { 0, 4, 4, 4, 4, 8, 8, 8, 32, 32 };

        public static int MAX_BLOCK_SIZE = Math.Min(65535, DEF_PENDING_BUFFER_SIZE - 5);

        public static int[] MAX_CHAIN = { 0, 4, 8, 32, 16, 32, 128, 256, 1024, 4096 };

        public static int[] MAX_LAZY = { 0, 4, 5, 6, 4, 16, 16, 32, 128, 258 };

        public static int[] NICE_LENGTH = { 0, 8, 16, 32, 16, 32, 128, 128, 258, 258 };

        private static short[] m_arrDistanceCodes;

        private static byte[] m_arrDistanceLengths;

        private short[] m_arrDistancesBuffer;

        private static short[] m_arrLiteralCodes;

        private static byte[] m_arrLiteralLengths;

        private byte[] m_arrLiteralsBuffer;

        private bool m_bCloseStream;

        private int m_BlockStart;

        private bool m_bNoWrap;

        private bool m_bStreamClosed;

        private long m_CheckSum = 1;

        private int m_CompressionFunction;

        private int m_CurrentHash;

        private byte[] m_DataWindow;

        private int m_GoodLength;

        private short[] m_HashHead;

        private short[] m_HashPrevious;

        private int m_iBufferPosition;

        private int m_iExtraBits;

        private byte[] m_InputBuffer;

        private int m_InputEnd;

        private int m_InputOffset;

        private CompressionLevel m_Level;

        private int m_LookAhead;

        private int m_MatchLength;

        private bool m_MatchPreviousAvailable;

        private int m_MatchStart;

        private int m_MaximumChainLength;

        private int m_MaximumLazySearch;

        private int m_NiceLength;

        private byte[] m_PendingBuffer = new byte[DEF_PENDING_BUFFER_SIZE];

        private uint m_PendingBufferBitsCache;

        private int m_PendingBufferBitsInCache;

        private int m_PendingBufferLength;

        private Stream m_stream;

        private int m_StringStart;

        private int m_TotalBytesIn;

        private CompressorHuffmanTree m_treeCodeLengths;

        private CompressorHuffmanTree m_treeDistances;

        private CompressorHuffmanTree m_treeLiteral;

        private enum BlockType
        {

            Stored = 0,

            FixedHuffmanCodes = 1,

            DynamicHuffmanCodes = 2
        }

        public int TotalIn
        {
            get
            {
                return m_TotalBytesIn;
            }
        }

        internal int PendingBufferBitCount
        {
            get
            {
                return m_PendingBufferBitsInCache;
            }
        }

        internal bool PendingBufferIsFlushed
        {
            get
            {
                return m_PendingBufferLength == 0;
            }
        }

        private bool HuffmanIsFull
        {
            get
            {
                return m_iBufferPosition >= DEF_HUFFMAN_BUFFER_SIZE;
            }
        }

        private bool NeedsInput
        {
            get
            {
                return m_InputEnd == m_InputOffset;
            }
        }

        public CompressedStreamWriter(Stream outputStream, bool bCloseStream)
            : this(outputStream, false, CompressionLevel.Normal, bCloseStream)
        {
        }

        public CompressedStreamWriter(Stream outputStream, bool bNoWrap, bool bCloseStream)
            : this(outputStream, bNoWrap, CompressionLevel.Normal, bCloseStream)
        {
        }

        public CompressedStreamWriter(Stream outputStream, bool bNoWrap, CompressionLevel level, bool bCloseStream)
        {
            if (outputStream == null)
                throw new ArgumentNullException("outputStream");

            if (!outputStream.CanWrite)
                throw new ArgumentException("Output stream does not support writing.", "outputStream");

            m_treeLiteral = new CompressorHuffmanTree(this, DEF_HUFFMAN_LITERAL_ALPHABET_LENGTH, 257, 15);
            m_treeDistances = new CompressorHuffmanTree(this, DEF_HUFFMAN_DISTANCES_ALPHABET_LENGTH, 1, 15);
            m_treeCodeLengths = new CompressorHuffmanTree(this, DEF_HUFFMAN_BITLEN_TREE_LENGTH, 4, 7);

            m_arrDistancesBuffer = new short[DEF_HUFFMAN_BUFFER_SIZE];
            m_arrLiteralsBuffer = new byte[DEF_HUFFMAN_BUFFER_SIZE];

            m_stream = outputStream;
            m_Level = level;
            m_bNoWrap = bNoWrap;
            m_bCloseStream = bCloseStream;

            m_DataWindow = new byte[2 * WSIZE];
            m_HashHead = new short[HASH_SIZE];
            m_HashPrevious = new short[WSIZE];
            m_BlockStart = m_StringStart = 1;

            m_GoodLength = GOOD_LENGTH[(int)level];
            m_MaximumLazySearch = MAX_LAZY[(int)level];
            m_NiceLength = NICE_LENGTH[(int)level];
            m_MaximumChainLength = MAX_CHAIN[(int)level];
            m_CompressionFunction = COMPR_FUNC[(int)level];

            if (!bNoWrap)
            {
                WriteZLIBHeader();
            }
        }

        public CompressedStreamWriter(Stream outputStream, CompressionLevel level, bool bCloseStream)
            : this(outputStream, false, level, bCloseStream)
        {
        }

        static CompressedStreamWriter()
        {
            m_arrLiteralCodes = new short[DEF_HUFFMAN_LITERAL_ALPHABET_LENGTH];
            m_arrLiteralLengths = new byte[DEF_HUFFMAN_LITERAL_ALPHABET_LENGTH];
            int i = 0;

            while (i < 144)
            {
                m_arrLiteralCodes[i] = BitUtils.BitReverse((0x030 + i) << 8);
                m_arrLiteralLengths[i++] = 8;
            }

            while (i < 256)
            {
                m_arrLiteralCodes[i] = BitUtils.BitReverse((0x190 - 144 + i) << 7);
                m_arrLiteralLengths[i++] = 9;
            }

            while (i < 280)
            {
                m_arrLiteralCodes[i] = BitUtils.BitReverse((0x000 - 256 + i) << 9);
                m_arrLiteralLengths[i++] = 7;
            }

            while (i < DEF_HUFFMAN_LITERAL_ALPHABET_LENGTH)
            {
                m_arrLiteralCodes[i] = BitUtils.BitReverse((0x0c0 - 280 + i) << 8);
                m_arrLiteralLengths[i++] = 8;
            }

            m_arrDistanceCodes = new short[DEF_HUFFMAN_DISTANCES_ALPHABET_LENGTH];
            m_arrDistanceLengths = new byte[DEF_HUFFMAN_DISTANCES_ALPHABET_LENGTH];

            for (i = 0; i < DEF_HUFFMAN_DISTANCES_ALPHABET_LENGTH; i++)
            {
                m_arrDistanceCodes[i] = BitUtils.BitReverse(i << 11);
                m_arrDistanceLengths[i] = 5;
            }
        }

        public void Write(byte[] data, int offset, int length, bool bCloseAfterWrite)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            int end = offset + length;

            if (0 > offset || offset > end || end > data.Length)
                throw new ArgumentOutOfRangeException("Offset or length is incorrect.");

            m_InputBuffer = data;
            m_InputOffset = offset;
            m_InputEnd = end;

            if (length == 0) return;

            if (m_bStreamClosed)
                throw new IOException("Stream was closed.");

            ChecksumCalculator.ChecksumUpdate(ref m_CheckSum, m_InputBuffer, m_InputOffset, length);

            while (!NeedsInput || !PendingBufferIsFlushed)
            {
                PendingBufferFlush();

                if (!CompressData(bCloseAfterWrite) && bCloseAfterWrite)
                {
                    PendingBufferFlush();
                    PendingBufferAlignToByte();

                    if (!m_bNoWrap)
                    {
                        PendingBufferWriteShortMSB((int)(m_CheckSum >> 16));
                        PendingBufferWriteShortMSB((int)(m_CheckSum & 0xffff));
                    }

                    PendingBufferFlush();
                    m_bStreamClosed = true;

                    if (m_bCloseStream)
                    {
                        m_stream.Close();
                    }
                }
            }
        }

        internal void PendingBufferAlignToByte()
        {
            if (m_PendingBufferBitsInCache > 0)
            {
                m_PendingBuffer[m_PendingBufferLength++] = (byte)m_PendingBufferBitsCache;

                if (m_PendingBufferBitsInCache > 8)
                {
                    m_PendingBuffer[m_PendingBufferLength++] = (byte)(m_PendingBufferBitsCache >> 8);
                }
            }

            m_PendingBufferBitsCache = 0;
            m_PendingBufferBitsInCache = 0;
        }

        internal void PendingBufferFlush()
        {
            PendingBufferFlushBits();

            m_stream.Write(m_PendingBuffer, 0, m_PendingBufferLength);
            m_PendingBufferLength = 0;
            m_stream.Flush();
        }

        internal int PendingBufferFlushBits()
        {
            int result = 0;

            while (m_PendingBufferBitsInCache >= 8 && m_PendingBufferLength < DEF_PENDING_BUFFER_SIZE)
            {
                m_PendingBuffer[m_PendingBufferLength++] = (byte)m_PendingBufferBitsCache;
                m_PendingBufferBitsCache >>= 8;
                m_PendingBufferBitsInCache -= 8;
                result++;
            }

            return result;
        }

        internal byte[] PendingBufferToByteArray()
        {
            byte[] result = new byte[m_PendingBufferLength - 0];
            Array.Copy(m_PendingBuffer, 0, result, 0, result.Length);
            m_PendingBufferLength = 0;

            return result;
        }

        internal void PendingBufferWriteBits(int b, int count)
        {
            m_PendingBufferBitsCache |= (uint)(b << m_PendingBufferBitsInCache);
            m_PendingBufferBitsInCache += count;

            PendingBufferFlushBits();
        }

        internal void PendingBufferWriteByte(int b)
        {
            Debug.Assert(m_PendingBufferBitsInCache == 0, "There is data in bits buffer.");
            m_PendingBuffer[m_PendingBufferLength++] = (byte)b;
        }

        internal void PendingBufferWriteByteBlock(byte[] data, int offset, int length)
        {
            Debug.Assert(m_PendingBufferBitsInCache == 0, "There is data in bits buffer.");
            Array.Copy(data, offset, m_PendingBuffer, m_PendingBufferLength, length);
            m_PendingBufferLength += length;
        }
        internal void PendingBufferWriteInt(int s)
        {
            Debug.Assert(m_PendingBufferBitsInCache == 0, "There is data in bits buffer.");
            m_PendingBuffer[m_PendingBufferLength++] = (byte)s;
            m_PendingBuffer[m_PendingBufferLength++] = (byte)(s >> 8);
            m_PendingBuffer[m_PendingBufferLength++] = (byte)(s >> 16);
            m_PendingBuffer[m_PendingBufferLength++] = (byte)(s >> 24);
        }
        internal void PendingBufferWriteShort(int s)
        {
            Debug.Assert(m_PendingBufferBitsInCache == 0, "There is data in bits buffer.");
            m_PendingBuffer[m_PendingBufferLength++] = (byte)s;
            m_PendingBuffer[m_PendingBufferLength++] = (byte)(s >> 8);
        }

        internal void PendingBufferWriteShortMSB(int s)
        {
            Debug.Assert(m_PendingBufferBitsInCache == 0, "There is data in bits buffer.");

            m_PendingBuffer[m_PendingBufferLength++] = (byte)(s >> 8);
            m_PendingBuffer[m_PendingBufferLength++] = (byte)s;
        }

        private bool CompressData(bool finish)
        {
            bool success;

            do
            {
                FillWindow();

                bool canFlush = (finish && NeedsInput);

                switch (m_CompressionFunction)
                {
                    case 0:
                        success = SaveStored(canFlush, finish);
                        break;

                    case 1:
                        success = CompressFast(canFlush, finish);
                        break;

                    case 2:
                        success = CompressSlow(canFlush, finish);
                        break;

                    default:
                        throw new InvalidOperationException("unknown m_CompressionFunction");
                }
            }
            while (PendingBufferIsFlushed && success);

            return success;
        }

        private bool CompressFast(bool flush, bool finish)
        {
            if (m_LookAhead < MIN_LOOKAHEAD && !flush)
            {
                return false;
            }

            while (m_LookAhead >= MIN_LOOKAHEAD || flush)
            {
                if (m_LookAhead == 0)
                {
                    HuffmanFlushBlock(m_DataWindow, m_BlockStart, m_StringStart - m_BlockStart, finish);
                    m_BlockStart = m_StringStart;
                    return false;
                }

                if (m_StringStart > 2 * WSIZE - MIN_LOOKAHEAD)
                {
                    SlideWindow();
                }

                int hashHead;

                if (m_LookAhead >= MIN_MATCH &&
                    (hashHead = InsertString()) != 0 &&
                    m_StringStart - hashHead <= MAX_DIST &&
                    FindLongestMatch(hashHead))
                {
                    if (HuffmanTallyDist(m_StringStart - m_MatchStart, m_MatchLength))
                    {
                        bool lastBlock = finish && m_LookAhead == 0;
                        HuffmanFlushBlock(m_DataWindow, m_BlockStart, m_StringStart - m_BlockStart, lastBlock);
                        m_BlockStart = m_StringStart;
                    }

                    m_LookAhead -= m_MatchLength;

                    if (m_MatchLength <= m_MaximumLazySearch && m_LookAhead >= MIN_MATCH)
                    {
                        while (--m_MatchLength > 0)
                        {
                            ++m_StringStart;
                            InsertString();
                        }

                        ++m_StringStart;
                    }
                    else
                    {
                        m_StringStart += m_MatchLength;

                        if (m_LookAhead >= MIN_MATCH - 1)
                        {
                            UpdateHash();
                        }
                    }

                    m_MatchLength = MIN_MATCH - 1;

                    continue;
                }
                else
                {
                    HuffmanTallyLit(m_DataWindow[m_StringStart] & 0xff);
                    ++m_StringStart;
                    --m_LookAhead;
                }

                if (HuffmanIsFull)
                {
                    bool lastBlock = (finish && m_LookAhead == 0);
                    HuffmanFlushBlock(m_DataWindow, m_BlockStart, m_StringStart - m_BlockStart, lastBlock);
                    m_BlockStart = m_StringStart;

                    return !lastBlock;
                }
            }
            return true;
        }

        private bool CompressSlow(bool flush, bool finish)
        {
            if (m_LookAhead < MIN_LOOKAHEAD && !flush)
            {
                return false;
            }

            while (m_LookAhead >= MIN_LOOKAHEAD || flush)
            {
                if (m_LookAhead == 0)
                {
                    if (m_MatchPreviousAvailable)
                    {
                        HuffmanTallyLit(m_DataWindow[m_StringStart - 1] & 0xff);
                    }

                    m_MatchPreviousAvailable = false;

                    HuffmanFlushBlock(m_DataWindow, m_BlockStart, m_StringStart - m_BlockStart, finish);

                    m_BlockStart = m_StringStart;

                    return false;
                }

                if (m_StringStart >= 2 * WSIZE - MIN_LOOKAHEAD)
                {
                    SlideWindow();
                }

                int prevMatch = m_MatchStart;
                int prevLen = m_MatchLength;

                if (m_LookAhead >= MIN_MATCH)
                {
                    int hashHead = InsertString();

                    if (hashHead != 0 && m_StringStart - hashHead <= MAX_DIST && FindLongestMatch(hashHead))
                    {
                        if (m_MatchLength <= 5 && (m_MatchLength == MIN_MATCH && m_StringStart - m_MatchStart > TOO_FAR))
                        {
                            m_MatchLength = MIN_MATCH - 1;
                        }
                    }
                }

                if (prevLen >= MIN_MATCH && m_MatchLength <= prevLen)
                {
                    HuffmanTallyDist(m_StringStart - 1 - prevMatch, prevLen);
                    prevLen -= 2;

                    do
                    {
                        m_StringStart++;
                        m_LookAhead--;

                        if (m_LookAhead >= MIN_MATCH)
                        {
                            InsertString();
                        }
                    }
                    while (--prevLen > 0);

                    m_StringStart++;
                    m_LookAhead--;
                    m_MatchPreviousAvailable = false;
                    m_MatchLength = MIN_MATCH - 1;
                }
                else
                {
                    if (m_MatchPreviousAvailable)
                    {
                        HuffmanTallyLit(m_DataWindow[m_StringStart - 1] & 0xff);
                    }

                    m_MatchPreviousAvailable = true;
                    m_StringStart++;
                    m_LookAhead--;
                }

                if (HuffmanIsFull)
                {
                    int len = m_StringStart - m_BlockStart;

                    if (m_MatchPreviousAvailable)
                    {
                        len--;
                    }

                    bool lastBlock = (finish && m_LookAhead == 0 && !m_MatchPreviousAvailable);
                    HuffmanFlushBlock(m_DataWindow, m_BlockStart, len, lastBlock);
                    m_BlockStart += len;

                    return !lastBlock;
                }
            }

            return true;
        }

        private void FillWindow()
        {
            if (m_StringStart >= WSIZE + MAX_DIST)
            {
                SlideWindow();
            }

            while (m_LookAhead < MIN_LOOKAHEAD && m_InputOffset < m_InputEnd)
            {
                int more = 2 * WSIZE - m_LookAhead - m_StringStart;

                if (more > m_InputEnd - m_InputOffset)
                {
                    more = m_InputEnd - m_InputOffset;
                }

                Array.Copy(m_InputBuffer, m_InputOffset, m_DataWindow, m_StringStart + m_LookAhead, more);

                m_InputOffset += more;
                m_TotalBytesIn += more;
                m_LookAhead += more;
            }

            if (m_LookAhead >= MIN_MATCH)
            {
                UpdateHash();
            }
        }

        private bool FindLongestMatch(int curMatch)
        {
            int chainLength = this.m_MaximumChainLength;
            int m_NiceLength = this.m_NiceLength;
            short[] m_HashPrevious = this.m_HashPrevious;
            int scan = this.m_StringStart;
            int match;
            int best_end = this.m_StringStart + m_MatchLength;
            int best_len = Math.Max(m_MatchLength, MIN_MATCH - 1);

            int limit = Math.Max(m_StringStart - MAX_DIST, 0);

            int strend = m_StringStart + MAX_MATCH - 1;
            byte scan_end1 = m_DataWindow[best_end - 1];
            byte scan_end = m_DataWindow[best_end];

            if (best_len >= this.m_GoodLength)
            {
                chainLength >>= 2;
            }

            if (m_NiceLength > m_LookAhead)
            {
                m_NiceLength = m_LookAhead;
            }

            do
            {
                if (m_DataWindow[curMatch + best_len] != scan_end ||
                    m_DataWindow[curMatch + best_len - 1] != scan_end1 ||
                    m_DataWindow[curMatch] != m_DataWindow[scan] ||
                    m_DataWindow[curMatch + 1] != m_DataWindow[scan + 1])
                {
                    continue;
                }

                match = curMatch + 2;
                scan += 2;

                while (m_DataWindow[++scan] == m_DataWindow[++match] &&
                       m_DataWindow[++scan] == m_DataWindow[++match] &&
                       m_DataWindow[++scan] == m_DataWindow[++match] &&
                       m_DataWindow[++scan] == m_DataWindow[++match] &&
                       m_DataWindow[++scan] == m_DataWindow[++match] &&
                       m_DataWindow[++scan] == m_DataWindow[++match] &&
                       m_DataWindow[++scan] == m_DataWindow[++match] &&
                       m_DataWindow[++scan] == m_DataWindow[++match] && scan < strend)
                {
                    ;
                }

                if (scan > best_end)
                {
                    m_MatchStart = curMatch;
                    best_end = scan;
                    best_len = scan - m_StringStart;

                    if (best_len >= m_NiceLength)
                    {
                        break;
                    }

                    scan_end1 = m_DataWindow[best_end - 1];
                    scan_end = m_DataWindow[best_end];
                }
                scan = m_StringStart;
            }
            while ((curMatch = (m_HashPrevious[curMatch & WMASK] & 0xffff)) > limit && --chainLength != 0);

            m_MatchLength = Math.Min(best_len, m_LookAhead);

            return m_MatchLength >= MIN_MATCH;
        }

        private void HuffmanCompressBlock()
        {
            for (int i = 0; i < m_iBufferPosition; i++)
            {
                int litlen = m_arrLiteralsBuffer[i] & 255;
                int dist = m_arrDistancesBuffer[i];

                if (dist-- != 0)
                {
                    int lc = HuffmanLengthCode(litlen);
                    m_treeLiteral.WriteCodeToStream(lc);

                    int bits = (lc - 261) / 4;
                    if (bits > 0 && bits <= 5)
                    {
                        PendingBufferWriteBits(litlen & ((1 << bits) - 1), bits);
                    }

                    int dc = HuffmanDistanceCode(dist);
                    m_treeDistances.WriteCodeToStream(dc);

                    bits = dc / 2 - 1;
                    if (bits > 0)
                    {
                        PendingBufferWriteBits(dist & ((1 << bits) - 1), bits);
                    }
                }
                else
                {
                    m_treeLiteral.WriteCodeToStream(litlen);
                }
            }

            m_treeLiteral.WriteCodeToStream(DEF_HUFFMAN_ENDBLOCK_SYMBOL);
        }

        private int HuffmanDistanceCode(int distance)
        {
            int code = 0;

            while (distance >= 4)
            {
                code += 2;
                distance >>= 1;
            }
            return code + distance;
        }

        private void HuffmanFlushBlock(byte[] stored, int storedOffset, int storedLength, bool lastBlock)
        {
            m_treeLiteral.CodeFrequences[DEF_HUFFMAN_ENDBLOCK_SYMBOL]++;

            m_treeLiteral.BuildTree();
            m_treeDistances.BuildTree();

            m_treeLiteral.CalcBLFreq(m_treeCodeLengths);
            m_treeDistances.CalcBLFreq(m_treeCodeLengths);

            m_treeCodeLengths.BuildTree();

            int blTreeCodes = 4;
            for (int i = 18; i > blTreeCodes; i--)
            {
                if (m_treeCodeLengths.CodeLengths[BitUtils.DEF_HUFFMAN_DYNTREE_CODELENGTHS_ORDER[i]] > 0)
                {
                    blTreeCodes = i + 1;
                }
            }
            int opt_len = 14 + blTreeCodes * 3 + m_treeCodeLengths.GetEncodedLength() +
                          m_treeLiteral.GetEncodedLength() + m_treeDistances.GetEncodedLength() +
                          m_iExtraBits;

            int static_len = m_iExtraBits;
            for (int i = 0; i < DEF_HUFFMAN_LITERAL_ALPHABET_LENGTH; i++)
            {
                static_len += m_treeLiteral.CodeFrequences[i] * m_arrLiteralLengths[i];
            }
            for (int i = 0; i < DEF_HUFFMAN_DISTANCES_ALPHABET_LENGTH; i++)
            {
                static_len += m_treeDistances.CodeFrequences[i] * m_arrDistanceLengths[i];
            }
            if (opt_len >= static_len)
            {
                opt_len = static_len;
            }

            if (storedOffset >= 0 && storedLength + 4 < opt_len >> 3)
            {
                HuffmanFlushStoredBlock(stored, storedOffset, storedLength, lastBlock);
            }
            else if (opt_len == static_len)
            {
                PendingBufferWriteBits(((int)BlockType.FixedHuffmanCodes << 1) + (lastBlock ? 1 : 0), 3);
                m_treeLiteral.SetStaticCodes(m_arrLiteralCodes, m_arrLiteralLengths);
                m_treeDistances.SetStaticCodes(m_arrDistanceCodes, m_arrDistanceLengths);
                HuffmanCompressBlock();
                HuffmanReset();
            }
            else
            {
                PendingBufferWriteBits(((int)BlockType.DynamicHuffmanCodes << 1) + (lastBlock ? 1 : 0), 3);
                HuffmanSendAllTrees(blTreeCodes);
                HuffmanCompressBlock();
                HuffmanReset();
            }
        }

        private void HuffmanFlushStoredBlock(byte[] stored, int storedOffset, int storedLength, bool lastBlock)
        {
            PendingBufferWriteBits(((int)BlockType.Stored << 1) + (lastBlock ? 1 : 0), 3);
            PendingBufferAlignToByte();
            PendingBufferWriteShort(storedLength);
            PendingBufferWriteShort(~storedLength);
            PendingBufferWriteByteBlock(stored, storedOffset, storedLength);
            HuffmanReset();
        }

        private int HuffmanLengthCode(int len)
        {
            if (len == 255)
            {
                return 285;
            }

            int code = 257;

            while (len >= 8)
            {
                code += 4;
                len >>= 1;
            }

            return code + len;
        }

        private void HuffmanReset()
        {
            m_iBufferPosition = 0;
            m_iExtraBits = 0;
            m_treeLiteral.Reset();
            m_treeDistances.Reset();
            m_treeCodeLengths.Reset();
        }

        private void HuffmanSendAllTrees(int blTreeCodes)
        {
            m_treeCodeLengths.BuildCodes();
            m_treeLiteral.BuildCodes();
            m_treeDistances.BuildCodes();
            PendingBufferWriteBits(m_treeLiteral.TreeLength - 257, 5);
            PendingBufferWriteBits(m_treeDistances.TreeLength - 1, 5);
            PendingBufferWriteBits(blTreeCodes - 4, 4);

            for (int rank = 0; rank < blTreeCodes; rank++)
            {
                PendingBufferWriteBits(
                    m_treeCodeLengths.CodeLengths[BitUtils.DEF_HUFFMAN_DYNTREE_CODELENGTHS_ORDER[rank]]
                    , 3);
            }

            m_treeLiteral.WriteTree(m_treeCodeLengths);
            m_treeDistances.WriteTree(m_treeCodeLengths);
        }
        private bool HuffmanTallyDist(int dist, int len)
        {
            m_arrDistancesBuffer[m_iBufferPosition] = (short)dist;
            m_arrLiteralsBuffer[m_iBufferPosition++] = (byte)(len - 3);

            int lc = HuffmanLengthCode(len - 3);
            m_treeLiteral.CodeFrequences[lc]++;
            if (lc >= 265 && lc < 285)
            {
                m_iExtraBits += (lc - 261) / 4;
            }

            int dc = HuffmanDistanceCode(dist - 1);
            m_treeDistances.CodeFrequences[dc]++;
            if (dc >= 4)
            {
                m_iExtraBits += dc / 2 - 1;
            }
            return HuffmanIsFull;
        }

        private bool HuffmanTallyLit(int literal)
        {
            m_arrDistancesBuffer[m_iBufferPosition] = 0;
            m_arrLiteralsBuffer[m_iBufferPosition++] = (byte)literal;
            m_treeLiteral.CodeFrequences[literal]++;
            return HuffmanIsFull;
        }

        private int InsertString()
        {
            short match;
            int hash = ((m_CurrentHash << HASH_SHIFT) ^ m_DataWindow[m_StringStart + (MIN_MATCH - 1)]) & HASH_MASK;

            m_HashPrevious[m_StringStart & WMASK] = match = m_HashHead[hash];
            m_HashHead[hash] = (short)m_StringStart;
            m_CurrentHash = hash;

            return match & 0xffff;
        }

        private bool SaveStored(bool flush, bool finish)
        {
            if (!flush && m_LookAhead == 0)
            {
                return false;
            }

            m_StringStart += m_LookAhead;
            m_LookAhead = 0;

            int storedLen = m_StringStart - m_BlockStart;

            if ((storedLen >= MAX_BLOCK_SIZE) ||
                (m_BlockStart < WSIZE && storedLen >= MAX_DIST) ||
                flush)
            {
                bool lastBlock = finish;

                if (storedLen > MAX_BLOCK_SIZE)
                {
                    storedLen = MAX_BLOCK_SIZE;
                    lastBlock = false;
                }

                HuffmanFlushStoredBlock(m_DataWindow, m_BlockStart, storedLen, lastBlock);
                m_BlockStart += storedLen;

                return !lastBlock;
            }
            return true;
        }

        private void SlideWindow()
        {
            Array.Copy(m_DataWindow, WSIZE, m_DataWindow, 0, WSIZE);
            m_MatchStart -= WSIZE;
            m_StringStart -= WSIZE;
            m_BlockStart -= WSIZE;

            for (int i = 0; i < HASH_SIZE; ++i)
            {
                int m = m_HashHead[i] & 0xffff;
                m_HashHead[i] = (short)((m >= WSIZE) ? (m - WSIZE) : 0);
            }

            for (int i = 0; i < WSIZE; i++)
            {
                int m = m_HashPrevious[i] & 0xffff;
                m_HashPrevious[i] = (short)((m >= WSIZE) ? (m - WSIZE) : 0);
            }
        }

        private void UpdateHash()
        {
            m_CurrentHash = (m_DataWindow[m_StringStart] << HASH_SHIFT) ^ m_DataWindow[m_StringStart + 1];
        }

        private void WriteZLIBHeader()
        {
            int iHeaderData = DEF_ZLIB_HEADER_TEMPLATE;

            iHeaderData |= (((int)m_Level >> 2) & 3) << 6;

            iHeaderData += 31 - (iHeaderData % 31);

            PendingBufferWriteShortMSB(iHeaderData);
        }
    }
}