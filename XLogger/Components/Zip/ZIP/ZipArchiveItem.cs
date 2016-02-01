using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace XLogger.Components.Zip.ZIP
{
    public class ZipArchiveItem : IDisposable
    {
        private bool m_bCheckCrc;
        private bool m_bCompressed;
        private bool m_bControlStream;
        private CompressionLevel m_compressionLevel = CompressionLevel.Normal;
        private CompressionMethod m_compressionMethod = CompressionMethod.Deflated;
        private int m_iExternalAttributes;
        private int m_iLocalHeaderOffset;
        private long m_lCompressedSize;
        private long m_lCrcPosition;
        private long m_lOriginalSize;
        private GeneralPurposeBitFlags m_options;
        private Stream m_streamData;
        private string m_strItemName;
        private uint m_uiCrc32;
        public bool Compressed
        {
            get
            {
                return m_bCompressed;
            }
        }
        public long CompressedSize
        {
            get
            {
                return m_lCompressedSize;
            }
        }

        public CompressionLevel CompressionLevel
        {
            get
            {
                return m_compressionLevel;
            }
            set
            {
                if (m_compressionLevel != value)
                {
                    if (m_bCompressed)
                    {
                        DecompressData();
                    }

                    m_compressionLevel = value;
                }
            }
        }

        public CompressionMethod CompressionMethod
        {
            get
            {
                return m_compressionMethod;
            }
        }
        public bool ControlStream
        {
            get
            {
                return m_bControlStream;
            }
        }
        public uint Crc32
        {
            get
            {
                return m_uiCrc32;
            }
        }
        public Stream DataStream
        {
            get
            {
                if (m_bCompressed)
                    DecompressData();

                return m_streamData;
            }
        }
        public FileAttributes ExternalAttributes
        {
            get
            {
                return (FileAttributes)m_iExternalAttributes;
            }
            set
            {
                m_iExternalAttributes = (int)value;
            }
        }
        public string ItemName
        {
            get
            {
                return m_strItemName;
            }
            set
            {
                if (value == null || value.Length == 0)
                    throw new ArgumentOutOfRangeException("ItemName");

                m_strItemName = value;
            }
        }
        public long OriginalSize
        {
            get
            {
                return m_lOriginalSize;
            }
        }

        public ZipArchiveItem(string itemName, Stream streamData, bool controlStream, FileAttributes attributes)
        {
            m_strItemName = itemName;
            m_bControlStream = controlStream;
            m_streamData = streamData;
            m_iExternalAttributes = (int)attributes;
        }
        internal ZipArchiveItem()
        {
        }
        public void ResetFlags()
        {
            m_lCompressedSize = 0;
            m_lOriginalSize = 0;
            m_bCompressed = false;
            m_uiCrc32 = 0;
        }
        public void Update(Stream newDataStream, bool controlStream)
        {
            if (m_streamData != null && m_bControlStream)
            {
                m_streamData.Close();
            }

            m_bControlStream = controlStream;
            m_streamData = newDataStream;
            ResetFlags();

            m_lOriginalSize = newDataStream.Length;
            m_compressionMethod = CompressionMethod.Deflated;
        }
        internal void Close()
        {
            if (m_streamData != null)
            {
                m_streamData.Flush();

                if (m_bControlStream)
                {
                    m_streamData.Close();
                }

                m_streamData = null;
                m_strItemName = null;
            }
        }

        internal void ReadCentralDirectoryData(Stream stream)
        {
            stream.Position += 4;

            m_options = (GeneralPurposeBitFlags)ZipArchive.ReadInt16(stream);
            m_compressionMethod = (CompressionMethod)ZipArchive.ReadInt16(stream);
            m_bCompressed = true;

            stream.Position += 4;

            m_uiCrc32 = (uint)ZipArchive.ReadInt32(stream);
            m_lCompressedSize = ZipArchive.ReadInt32(stream);
            m_lOriginalSize = ZipArchive.ReadInt32(stream);

            int iFileNameLength = ZipArchive.ReadInt16(stream);
            int iExtraFieldLenth = ZipArchive.ReadInt16(stream);
            int iCommentLength = ZipArchive.ReadInt16(stream);

            stream.Position += 4;

            m_iExternalAttributes = ZipArchive.ReadInt32(stream);
            m_iLocalHeaderOffset = ZipArchive.ReadInt32(stream);

            byte[] arrBuffer = new byte[iFileNameLength];

            stream.Read(arrBuffer, 0, iFileNameLength);

            Encoding encoding = ((m_options & GeneralPurposeBitFlags.Unicode) != 0) ?
              Encoding.UTF8 :
              Encoding.Default;

            m_strItemName = encoding.GetString(arrBuffer, 0, arrBuffer.Length);

            stream.Position += iExtraFieldLenth + iCommentLength;
        }
        internal void ReadData(Stream stream, bool checkCrc)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            stream.Position = m_iLocalHeaderOffset;
            m_bCheckCrc = checkCrc;

            ReadLocalHeader(stream);
            ReadCompressedData(stream);
        }
        internal void Write(Stream outputStream)
        {
            if (m_streamData == null || m_streamData.Length == 0)
            {
                m_compressionLevel = CompressionLevel.NoCompression;
                m_compressionMethod = CompressionMethod.Stored;
            }
            WriteHeader(outputStream);
            WriteZippedContent(outputStream);
            WriteFooter(outputStream);
        }
        internal void WriteFileHeader(Stream stream)
        {
            stream.Write(BitConverter.GetBytes(Constants.CentralHeaderSignature), 0, Constants.IntSize);
            stream.Write(BitConverter.GetBytes((short)Constants.VersionMadeBy), 0, Constants.ShortSize);
            stream.Write(BitConverter.GetBytes((short)Constants.VersionNeededToExtract), 0, Constants.ShortSize);

            stream.Write(BitConverter.GetBytes((short)m_options), 0, Constants.ShortSize);

            stream.Write(BitConverter.GetBytes((short)m_compressionMethod),
              0, Constants.ShortSize);

            stream.WriteByte(0);
            stream.WriteByte(0);

            stream.WriteByte(33);
            stream.WriteByte(0);

            stream.Write(BitConverter.GetBytes(m_uiCrc32), 0, Constants.IntSize);
            stream.Write(BitConverter.GetBytes((int)m_lCompressedSize), 0, Constants.IntSize);
            stream.Write(BitConverter.GetBytes((int)m_lOriginalSize), 0, Constants.IntSize);

            Encoding encoding = ((m_options & GeneralPurposeBitFlags.Unicode) != 0) ?
              Encoding.UTF8 :
              Encoding.Default;

            int iNameLength = encoding.GetByteCount(m_strItemName);

            stream.Write(BitConverter.GetBytes((short)iNameLength), 0, Constants.ShortSize);

            stream.WriteByte(0);
            stream.WriteByte(0);

            stream.WriteByte(0);
            stream.WriteByte(0);

            stream.WriteByte(0);
            stream.WriteByte(0);

            stream.WriteByte(0);
            stream.WriteByte(0);

            stream.Write(BitConverter.GetBytes(m_iExternalAttributes), 0, Constants.IntSize);

            stream.Write(BitConverter.GetBytes(m_iLocalHeaderOffset), 0, Constants.IntSize);

            byte[] arrName = encoding.GetBytes(m_strItemName);
            stream.Write(arrName, 0, arrName.Length);
        }
        private void CheckCrc()
        {
            m_streamData.Position = 0;
            uint uiCrc = ZipCrc32.ComputeCrc(m_streamData, (int)m_lOriginalSize);

            if (uiCrc != m_uiCrc32)
                throw new ZipException("Wrong Crc value.");
        }
        private void DecompressData()
        {
            if (m_bCompressed)
            {
                if (m_compressionMethod == CompressionMethod.Deflated)
                {
                    if (m_lOriginalSize > 0)
                    {
                        m_streamData.Position = 0;
                        DeflateStream deflateStream = new DeflateStream(m_streamData, CompressionMode.Decompress, true);
                        MemoryStream decompressedData = new MemoryStream();
                        decompressedData.Capacity = (int)m_lOriginalSize;
                        byte[] arrBuffer = new byte[Constants.BufferSize];
                        int iReadBytes;

                        while ((iReadBytes = deflateStream.Read(arrBuffer, 0, Constants.BufferSize)) > 0)
                        {
                            decompressedData.Write(arrBuffer, 0, iReadBytes);
                        }

                        deflateStream.Dispose();

                        if (m_bControlStream)
                        {
                            m_streamData.Close();
                        }

                        m_bControlStream = true;
                        m_streamData = decompressedData;
                        decompressedData.SetLength(m_lOriginalSize);
                        decompressedData.Capacity = (int)m_lOriginalSize;

                        if (m_bCheckCrc) CheckCrc();

                        m_streamData.Position = 0;
                    }

                    m_bCompressed = false;
                }
                else if (m_compressionMethod == CompressionMethod.Stored)
                {
                    m_bCompressed = false;
                }
                else
                {
                    throw new NotSupportedException("Compression type: " + m_compressionMethod.ToString() + " is not supported");
                }
            }
        }
        private void ReadCompressedData(Stream stream)
        {
            if (m_lCompressedSize > 0)
            {
                MemoryStream dataStream = new MemoryStream();
                int iBytesLeft = (int)m_lCompressedSize;
                dataStream.Capacity = iBytesLeft;

                byte[] arrBuffer = new byte[Constants.BufferSize];

                while (iBytesLeft > 0)
                {
                    int iBytesToRead = Math.Min(iBytesLeft, Constants.BufferSize);

                    if (stream.Read(arrBuffer, 0, iBytesToRead) != iBytesToRead)
                        throw new ZipException("End of file reached - wrong file format or file is corrupt.");

                    dataStream.Write(arrBuffer, 0, iBytesToRead);
                    iBytesLeft -= iBytesToRead;
                }

                m_streamData = dataStream;
                m_bControlStream = true;
            }
        }
        private void ReadLocalHeader(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (ZipArchive.ReadInt32(stream) != Constants.HeaderSignature)
                throw new ZipException("Can't find local header signature - wrong file format or file is corrupt.");

            stream.Position += 22;

            int iNameLength = ZipArchive.ReadInt16(stream);
            int iExtraLength = ZipArchive.ReadInt16(stream);

            stream.Position += iNameLength + iExtraLength;
        }
        private void WriteFooter(Stream outputStream)
        {
            if (outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }

            long lCurrentPos = outputStream.Position;

            outputStream.Position = m_lCrcPosition;
            outputStream.Write(BitConverter.GetBytes(m_uiCrc32), 0, Constants.IntSize);
            outputStream.Write(BitConverter.GetBytes((int)m_lCompressedSize), 0, Constants.IntSize);
            outputStream.Write(BitConverter.GetBytes((int)m_lOriginalSize), 0, Constants.IntSize);

            outputStream.Position = lCurrentPos;
        }
        private void WriteHeader(Stream outputStream)
        {
            m_iLocalHeaderOffset = (int)outputStream.Position;
            outputStream.Write(BitConverter.GetBytes(Constants.HeaderSignature), 0, Constants.HeaderSignatureBytes);
            outputStream.Write(BitConverter.GetBytes(Constants.VersionNeededToExtract), 0, Constants.ShortSize);
            outputStream.Write(BitConverter.GetBytes((short)m_options), 0, Constants.ShortSize);
            outputStream.Write(BitConverter.GetBytes((short)m_compressionMethod),
              0, Constants.ShortSize);

            outputStream.WriteByte(0);
            outputStream.WriteByte(0);

            outputStream.WriteByte(33);
            outputStream.WriteByte(0);

            m_lCrcPosition = outputStream.Position;
            outputStream.Write(BitConverter.GetBytes(m_uiCrc32), 0, Constants.IntSize);
            outputStream.Write(BitConverter.GetBytes((int)m_lCompressedSize), 0, Constants.IntSize);
            outputStream.Write(BitConverter.GetBytes((int)m_lOriginalSize), 0, Constants.IntSize);

            Encoding encoding = ((m_options & GeneralPurposeBitFlags.Unicode) != 0) ?
              Encoding.UTF8 :
              Encoding.Default;

            int iCharCount = encoding.GetByteCount(m_strItemName);
            outputStream.Write(BitConverter.GetBytes((short)iCharCount), 0, Constants.ShortSize);

            outputStream.WriteByte(0);
            outputStream.WriteByte(0);

            byte[] arrName = encoding.GetBytes(m_strItemName);
            outputStream.Write(arrName, 0, arrName.Length);
        }
        private void WriteZippedContent(Stream outputStream)
        {
            long lDataLength = (m_streamData != null) ? m_streamData.Length : 0L;

            if (lDataLength <= 0)
            {
                return;
            }

            long lStartPosition = outputStream.Position;

            if (m_bCompressed || m_compressionMethod == CompressionMethod.Stored)
            {
                m_streamData.Position = 0;
                byte[] arrBuffer = new byte[Constants.BufferSize];

                while (lDataLength > 0)
                {
                    int iReadSize = m_streamData.Read(arrBuffer, 0, Constants.BufferSize);
                    outputStream.Write(arrBuffer, 0, iReadSize);
                    lDataLength -= iReadSize;
                }
            }
            else if (m_compressionMethod == CompressionMethod.Deflated)
            {
                m_lOriginalSize = lDataLength;
                m_streamData.Position = 0;
                m_uiCrc32 = 0;
                byte[] arrBuffer = new byte[Constants.BufferSize];
                DeflateStream deflateStream = new DeflateStream(outputStream, CompressionMode.Compress, true);
                while (lDataLength > 0)
                {
                    int iReadSize = m_streamData.Read(arrBuffer, 0, Constants.BufferSize);

                    deflateStream.Write(arrBuffer, 0, iReadSize);
                    lDataLength -= iReadSize;
                    m_uiCrc32 = ZipCrc32.ComputeCrc(arrBuffer, 0, iReadSize, m_uiCrc32);
                }

                deflateStream.Close();
            }

            m_lCompressedSize = outputStream.Position - lStartPosition;
        }

#region IDisposable Members
        public void Dispose()
        {
            if (m_strItemName != null)
            {
                Close();
                m_strItemName = null;
                GC.SuppressFinalize(this);
            }
        }

        ~ZipArchiveItem()
        {
            Dispose();
        }
#endregion

    }//class
    //i
    //class
}//ns
