using System;
using System.Diagnostics;
using System.IO;

namespace XLogger.Components.Zip
{

    public class CompressedStreamReader
    {

        private const int DEF_HEADER_FLAGS_FCHECK = 31;

        private const int DEF_HEADER_FLAGS_FDICT = 32;

        private const int DEF_HEADER_FLAGS_FLEVEL = 192;

        private const int DEF_HEADER_INFO_MASK = 240 << 8;

        private const int DEF_HEADER_METHOD_MASK = 15 << 8;
        private const int DEF_HUFFMAN_DISTANCE_MAXIMUM_CODE = 29;

        private static readonly int[] DEF_HUFFMAN_DYNTREE_REPEAT_BITS = { 2, 3, 7 };

        private static readonly int[] DEF_HUFFMAN_DYNTREE_REPEAT_MINIMUMS = { 3, 3, 11 };
        private const int DEF_HUFFMAN_END_BLOCK = 256;
        private const int DEF_HUFFMAN_LENGTH_MAXIMUM_CODE = 285;
        private const int DEF_HUFFMAN_LENGTH_MINIMUM_CODE = 257;

        private static readonly int[] DEF_HUFFMAN_REPEAT_DISTANCE_BASE =
      {
        1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193,
        257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145,
        8193, 12289, 16385, 24577
      };

        private static readonly int[] DEF_HUFFMAN_REPEAT_DISTANCE_EXTENSION =
      {
        0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6,
        7, 7, 8, 8, 9, 9, 10, 10, 11, 11,
        12, 12, 13, 13
      };

        private static readonly int[] DEF_HUFFMAN_REPEAT_LENGTH_BASE =
      {
        3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31,
        35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258
      };

        private static readonly int[] DEF_HUFFMAN_REPEAT_LENGTH_EXTENSION =
      {
        0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2,
        3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0
      };

        private const int DEF_HUFFMAN_REPEATE_MAX = 258;

        private const int DEF_MAX_WINDOW_SIZE = UInt16.MaxValue;
        private bool m_bCanReadMoreData = true;

        private bool m_bCanReadNextBlock = true;
        private bool m_bCheckSumRead;

        private byte[] m_Block_Buffer = new byte[DEF_MAX_WINDOW_SIZE];
        private bool m_bNoWrap;

        private bool m_bReadingUncompressed;
        private uint m_Buffer;

        private int m_BufferedBits;
        private long m_CheckSum = 1;
        private DecompressorHuffmanTree m_CurrentDistanceTree;
        private DecompressorHuffmanTree m_CurrentLengthTree;

        private long m_CurrentPosition;

        private long m_DataLength;
        private Stream m_InputStream;
        private byte[] m_temp_buffer = new byte[4];

        private int m_UncompressedDataLength;

        private int m_WindowSize;
        protected internal int AvailableBits
        {
            get
            {
                return m_BufferedBits;
            }
        }
        protected internal long AvailableBytes
        {
            get
            {
                return m_InputStream.Length - m_InputStream.Position + m_BufferedBits >> 3;
            }
        }
        public CompressedStreamReader(Stream stream)
            : this(stream, false)
        {
        }
        public CompressedStreamReader(Stream stream, bool bNoWrap)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (stream.Length == 0)
                throw new ArgumentException("stream - string can not be empty");

            m_InputStream = stream;
            m_bNoWrap = bNoWrap;

            if (!m_bNoWrap)
            {
                ReadZLibHeader();
            }

            DecodeBlockHeader();
        }
        public int Read(byte[] buffer, int offset, int length)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (offset < 0 || offset > buffer.Length - 1)
                throw new ArgumentOutOfRangeException("offset", offset, "Offset does not belong to specified buffer.");

            if (length < 0 || length > buffer.Length - offset)
                throw new ArgumentOutOfRangeException("length", length, "Length is illegal.");

            int initialLength = length;

            while (length > 0)
            {
                if (m_CurrentPosition < m_DataLength)
                {
                    int inBlockPosition = (int)(m_CurrentPosition % DEF_MAX_WINDOW_SIZE);
                    int dataToCopy = Math.Min(DEF_MAX_WINDOW_SIZE - inBlockPosition, (int)(m_DataLength - m_CurrentPosition));
                    dataToCopy = Math.Min(dataToCopy, length);
                    Array.Copy(m_Block_Buffer, inBlockPosition, buffer, offset, dataToCopy);
                    m_CurrentPosition += (long)dataToCopy;
                    offset += dataToCopy;
                    length -= dataToCopy;
                }
                else
                {
                    Debug.Assert(m_CurrentPosition == m_DataLength, "Wrong position",
                                  "Current position is lerger than data length, so something is wrong.");

                    if (!m_bCanReadMoreData)
                    {
                        break;
                    }

                    long oldDataLength = m_DataLength;

                    if (!m_bReadingUncompressed)
                    {
                        if (!ReadHuffman())
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (m_UncompressedDataLength == 0)
                        {
                            if (!(m_bCanReadMoreData = DecodeBlockHeader()))
                            {
                                break;
                            }
                        }
                        else
                        {
                            int inBlockPosition = (int)(m_DataLength % DEF_MAX_WINDOW_SIZE);
                            int dataToRead = Math.Min(m_UncompressedDataLength, DEF_MAX_WINDOW_SIZE - inBlockPosition);
                            int dataRead = ReadPackedBytes(m_Block_Buffer, inBlockPosition, dataToRead);

                            if (dataToRead != dataRead)
                                throw new FormatException("Not enough data in stream.");

                            m_UncompressedDataLength -= dataRead;

                            m_DataLength += (long)dataRead;
                        }
                    }

                    Debug.Assert((m_DataLength - m_CurrentPosition) < DEF_MAX_WINDOW_SIZE, "Wrong position",
                                  "Unread block is larger than 32k, this is wrong behaviour, because some data is lost.");

                    if (oldDataLength < m_DataLength)
                    {
                        int start = (int)(oldDataLength % DEF_MAX_WINDOW_SIZE);
                        int end = (int)(m_DataLength % DEF_MAX_WINDOW_SIZE);

                        if (start < end)
                        {
                            ChecksumUpdate(m_Block_Buffer, start, end - start);
                        }
                        else
                        {
                            ChecksumUpdate(m_Block_Buffer, start, DEF_MAX_WINDOW_SIZE - start);

                            if (end > 0)
                            {
                                ChecksumUpdate(m_Block_Buffer, 0, end);
                            }
                        }
                    }
                }
            }

            if (!m_bCanReadMoreData && !m_bCheckSumRead && !m_bNoWrap)
            {
                SkipToBoundary();
                long checkSum = ReadInt32();

                if (checkSum != m_CheckSum)
                    throw new Exception("Checksum check failed.");

                m_bCheckSumRead = true;
            }

            return initialLength - length;
        }
        protected string BitsToString(int bits, int count)
        {
            string result = "";

            for (int i = 0; i < count; i++)
            {
                if ((i & 7) == 0)
                {
                    result = " " + result;
                }

                result = (bits & 1).ToString() + result;
                bits >>= 1;
            }

            return result;
        }
        protected void ChecksumReset()
        {
            m_CheckSum = 1;
        }
        protected void ChecksumUpdate(byte[] buffer, int offset, int length)
        {
            ChecksumCalculator.ChecksumUpdate(ref m_CheckSum, buffer, offset, length);
        }
        protected bool DecodeBlockHeader()
        {
            if (!m_bCanReadNextBlock)
            {
                return false;
            }

            int bFinalBlock = ReadBits(1);
            if (bFinalBlock == -1)
            {
                return false;
            }

            int blockType = ReadBits(2);
            if (blockType == -1)
            {
                return false;
            }

            m_bCanReadNextBlock = (bFinalBlock == 0);
            switch (blockType)
            {
                case 0:
                    m_bReadingUncompressed = true;

                    SkipToBoundary();
                    int length = ReadInt16Inverted();
                    int lengthComplement = ReadInt16Inverted();

                    if (length != (lengthComplement ^ 0xffff))
                        throw new FormatException("Wrong block length.");

                    if (length > UInt16.MaxValue)
                        throw new FormatException("Uncompressed block length can not be more than 65535.");

                    m_UncompressedDataLength = length;
                    m_CurrentLengthTree = null;
                    m_CurrentDistanceTree = null;
                    break;

                case 1:
                    m_bReadingUncompressed = false;
                    m_UncompressedDataLength = -1;
                    m_CurrentLengthTree = DecompressorHuffmanTree.LengthTree;
                    m_CurrentDistanceTree = DecompressorHuffmanTree.DistanceTree;
                    break;

                case 2:
                    m_bReadingUncompressed = false;
                    m_UncompressedDataLength = -1;
                    DecodeDynHeader(out m_CurrentLengthTree, out m_CurrentDistanceTree);
                    break;

                default:
                    throw new FormatException("Wrong block type.");
            }

            return true;
        }
        protected void DecodeDynHeader(out DecompressorHuffmanTree lengthTree, out DecompressorHuffmanTree distanceTree)
        {
            byte[] arrDecoderCodeLengths;
            byte[] arrResultingCodeLengths;

            byte bLastSymbol = 0;
            int iLengthsCount = ReadBits(5);
            int iDistancesCount = ReadBits(5);
            int iCodeLengthsCount = ReadBits(4);

            if (iLengthsCount < 0 || iDistancesCount < 0 || iCodeLengthsCount < 0)
                throw new FormatException("Wrong dynamic huffman codes.");

            iLengthsCount += 257;
            iDistancesCount += 1;

            int iResultingCodeLengthsCount = iLengthsCount + iDistancesCount;
            arrResultingCodeLengths = new byte[iResultingCodeLengthsCount];
            arrDecoderCodeLengths = new byte[19];
            iCodeLengthsCount += 4;
            int iCurrentCode = 0;

            while (iCurrentCode < iCodeLengthsCount)
            {
                int len = ReadBits(3);

                if (len < 0)
                    throw new FormatException("Wrong dynamic huffman codes.");

                arrDecoderCodeLengths[BitUtils.DEF_HUFFMAN_DYNTREE_CODELENGTHS_ORDER[iCurrentCode++]] =
                  (byte)len;
            }

            DecompressorHuffmanTree treeInternalDecoder = new
              DecompressorHuffmanTree(arrDecoderCodeLengths);

            iCurrentCode = 0;

            for (; ; )
            {
                int symbol;
                bool bNeedBreak = false;

                while (((symbol = treeInternalDecoder.UnpackSymbol(this)) & ~15) == 0)
                {
                    arrResultingCodeLengths[iCurrentCode++] = bLastSymbol = (byte)symbol;

                    if (iCurrentCode == iResultingCodeLengthsCount)
                    {
                        bNeedBreak = true;
                        break;
                    }
                }

                if (bNeedBreak) break;

                if (symbol < 0)
                    throw new FormatException("Wrong dynamic huffman codes.");

                if (symbol >= 17)
                {
                    bLastSymbol = 0;
                }
                else if (iCurrentCode == 0)
                {
                    throw new FormatException("Wrong dynamic huffman codes.");
                }

                int m_iRepSymbol = symbol - 16;
                int bits = DEF_HUFFMAN_DYNTREE_REPEAT_BITS[m_iRepSymbol];

                int count = ReadBits(bits);

                if (count < 0)
                    throw new FormatException("Wrong dynamic huffman codes.");

                count += DEF_HUFFMAN_DYNTREE_REPEAT_MINIMUMS[m_iRepSymbol];

                if (iCurrentCode + count > iResultingCodeLengthsCount)
                    throw new FormatException("Wrong dynamic huffman codes.");

                while (count-- > 0)
                {
                    arrResultingCodeLengths[iCurrentCode++] = bLastSymbol;
                }

                if (iCurrentCode == iResultingCodeLengthsCount) break;
            }

            byte[] tempArray = new byte[iLengthsCount];
            Array.Copy(arrResultingCodeLengths, 0, tempArray, 0, iLengthsCount);
            lengthTree = new DecompressorHuffmanTree(tempArray);

            tempArray = new byte[iDistancesCount];
            Array.Copy(arrResultingCodeLengths, iLengthsCount, tempArray, 0, iDistancesCount);
            distanceTree = new DecompressorHuffmanTree(tempArray);

        }

        protected void FillBuffer()
        {
            int length = 4 - (m_BufferedBits >> 3) -
              (((m_BufferedBits & 7) != 0) ? 1 : 0);

            if (length == 0)
            {
                return;
            }

            int bytesRead = m_InputStream.Read(m_temp_buffer, 0, length);

            for (int i = 0; i < bytesRead; i++)
            {
                m_Buffer |= ((uint)m_temp_buffer[i] << m_BufferedBits);
                m_BufferedBits += 8;
            }
        }
        protected void ReadZLibHeader()
        {
            int header = ReadInt16();

            if (header == -1)
                throw new Exception("Header of the stream can not be read.");

            if (header % 31 != 0)
                throw new FormatException("Header checksum illegal");

            if ((header & DEF_HEADER_METHOD_MASK) != (8 << 8))
                throw new FormatException("Unsupported compression method.");

            m_WindowSize = (int)Math.Pow(2, ((header & DEF_HEADER_INFO_MASK) >> 12) + 8);

            if (m_WindowSize > UInt16.MaxValue)
                throw new FormatException("Unsupported window size for deflate compression method.");

            if ((header & DEF_HEADER_FLAGS_FDICT) >> 5 == 1)
            {
                throw new NotImplementedException("not supported");
            }

        }

        protected internal int PeekBits(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", count, "Bits count can not be less than zero.");

            if (count > 32)
                throw new ArgumentOutOfRangeException("count", count, "Count of bits is too large.");
            if (m_BufferedBits < count)
            {
                FillBuffer();
            }
            if (m_BufferedBits < count)
            {
                return -1;
            }

            uint bitMask = ~(uint.MaxValue << count);

            int result = (int)(m_Buffer & bitMask);

            return result;
        }

        protected internal int ReadBits(int count)
        {
            int result = PeekBits(count);

            if (result == -1)
            {
                return -1;
            }

            m_BufferedBits -= count;
            m_Buffer >>= count;
            return result;
        }

        protected internal int ReadInt16()
        {
            int result = (ReadBits(8) << 8);
            result |= ReadBits(8);
            return result;
        }
        protected internal int ReadInt16Inverted()
        {
            int result = (ReadBits(8));
            result |= ReadBits(8) << 8;
            return result;
        }
        protected internal long ReadInt32()
        {
            long result = (uint)(ReadBits(8) << 24);
            result |= (uint)(ReadBits(8) << 16);
            result |= (uint)(ReadBits(8) << 8);
            result |= (uint)ReadBits(8);
            return result;
        }
        protected internal int ReadPackedBytes(byte[] buffer, int offset, int length)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (offset < 0 || offset > buffer.Length - 1)
                throw new ArgumentOutOfRangeException("offset", offset, "Offset can not be less than zero or greater than buffer length - 1.");

            if (length < 0)
                throw new ArgumentOutOfRangeException("length", length, "Length can not be less than zero.");

            if (length > buffer.Length - offset)
                throw new ArgumentOutOfRangeException("length", length, "Length is too large.");

            if ((m_BufferedBits & 7) != 0)
                throw new NotSupportedException("Reading of unalligned data is not supported.");

            if (length == 0) return 0;

            int result = 0;

            while (m_BufferedBits > 0 && length > 0)
            {
                buffer[offset++] = (byte)(m_Buffer);
                m_BufferedBits -= 8;
                m_Buffer >>= 8;
                length--;
                result++;
            }

            if (length > 0)
            {
                result += m_InputStream.Read(buffer, offset, length);
            }

            return result;
        }

        protected internal void SkipBits(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", count, "Bits count can not be less than zero.");

            if (count == 0)
                return;

            if (count >= m_BufferedBits)
            {
                count -= m_BufferedBits;
                m_BufferedBits = 0;
                m_Buffer = 0;

                if (count > 0)
                {
                    m_InputStream.Position += (count >> 3);
                    count &= 7;

                    if (count > 0)
                    {
                        FillBuffer();
                        m_BufferedBits -= count;
                        m_Buffer >>= count;
                    }
                }
            }
            else
            {
                m_BufferedBits -= count;
                m_Buffer >>= count;
            }
        }
        protected internal void SkipToBoundary()
        {
            m_Buffer >>= (m_BufferedBits & 7);
            m_BufferedBits &= ~7;
        }
        private bool ReadHuffman()
        {
            int free = DEF_MAX_WINDOW_SIZE - (int)(m_DataLength - m_CurrentPosition);
            bool dataRead = false;
            while (free >= DEF_HUFFMAN_REPEATE_MAX)
            {
                int symbol;
                while (((symbol = m_CurrentLengthTree.UnpackSymbol(this)) & ~0xff) == 0)
                {
                    m_Block_Buffer[m_DataLength++ % DEF_MAX_WINDOW_SIZE] = (byte)symbol;
                    dataRead = true;

                    if (--free < DEF_HUFFMAN_REPEATE_MAX)
                    {
                        return true;
                    }
                }

                if (symbol < DEF_HUFFMAN_LENGTH_MINIMUM_CODE)
                {
                    if (symbol < DEF_HUFFMAN_END_BLOCK)
                        throw new FormatException("Illegal code.");

                    return (dataRead | (m_bCanReadMoreData = DecodeBlockHeader()));
                }

                if (symbol > DEF_HUFFMAN_LENGTH_MAXIMUM_CODE)
                    throw new FormatException("Illegal repeat code length.");

                int iRepeatLength = DEF_HUFFMAN_REPEAT_LENGTH_BASE
                  [symbol - DEF_HUFFMAN_LENGTH_MINIMUM_CODE];

                int iRepeatExtraBits = DEF_HUFFMAN_REPEAT_LENGTH_EXTENSION
                  [symbol - DEF_HUFFMAN_LENGTH_MINIMUM_CODE];

                if (iRepeatExtraBits > 0)
                {
                    int extra = ReadBits(iRepeatExtraBits);

                    if (extra < 0)
                        throw new FormatException("Wrong data.");

                    iRepeatLength += extra;
                }

                symbol = m_CurrentDistanceTree.UnpackSymbol(this);

                if (symbol < 0 || symbol > DEF_HUFFMAN_REPEAT_DISTANCE_BASE.Length)
                    throw new FormatException("Wrong distance code.");

                int iRepeatDistance = DEF_HUFFMAN_REPEAT_DISTANCE_BASE[symbol];
                iRepeatExtraBits = DEF_HUFFMAN_REPEAT_DISTANCE_EXTENSION[symbol];

                if (iRepeatExtraBits > 0)
                {
                    int extra = ReadBits(iRepeatExtraBits);

                    if (extra < 0)
                        throw new FormatException("Wrong data.");

                    iRepeatDistance += extra;
                }

                for (int i = 0; i < iRepeatLength; i++)
                {
                    m_Block_Buffer[m_DataLength % DEF_MAX_WINDOW_SIZE] =
                      m_Block_Buffer[(m_DataLength - (long)iRepeatDistance) % DEF_MAX_WINDOW_SIZE];
                    m_DataLength++;
                    free--;
                }

                dataRead = true;

            }

            return dataRead;
        }
    }//class

    //class

#if WindowsCE
    public class ArgumentOutOfRangeException : System.ArgumentOutOfRangeException
    {
        /// <summary>
        /// Initializes a new instance of the ArgumentOutOfRangeException class. 
        /// </summary>
        public ArgumentOutOfRangeException()
        {
        }


        /// <summary>
        /// Initializes a new instance of the ArgumentOutOfRangeException class with the name of the parameter that causes this exception.
        /// </summary>
        /// <param name="paramName">The name of the parameter that causes this exception.</param>
        public ArgumentOutOfRangeException(string paramName)
            : base(paramName)
        {
        }


        /// <summary>
        /// Initializes a new instance of the ArgumentOutOfRangeException class with a specified error message and the name of the parameter that causes this exception.
        /// </summary>
        /// <param name="paramName">The name of the parameter that caused the exception.</param>
        /// <param name="message">The message that describes the error.</param>
        public ArgumentOutOfRangeException(string paramName, string message)
        {
        }


        /// <summary>
        /// Initializes a new instance of the ArgumentOutOfRangeException class with a specified error message, the parameter name, and the value of the argument.
        /// </summary>
        /// <param name="paramName">The name of the parameter that caused the exception.</param>
        /// <param name="actualValue">The value of the argument that causes this exception.</param>
        /// <param name="message">The message that describes the error.</param>
        public ArgumentOutOfRangeException(string paramName, Object actualValue, string message)
            : base(string.Format("{0}={1}", paramName, actualValue), message)
        {
        }
    }
#endif
 
}//ns
