using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;

namespace XLogger.Components.Zip.ZIP
{
  
  public class ZipArchive : IDisposable
  {
      private ArrayList m_arrItems = new ArrayList();
      private bool m_bCheckCrc = true;
      private CompressionLevel m_defaultLevel = CompressionLevel.Best;
      private Hashtable m_dicItems = new Hashtable();
      private IFileNamePreprocessor m_fileNamePreprocessor;
      private static byte[] s_tempBuffer = new byte[Constants.IntSize];
      public bool CheckCrc
      {
          get
          {
              return m_bCheckCrc;
          }
          set
          {
              m_bCheckCrc = value;
          }
      }
      public int Count
      {
          get
          {
              return (m_arrItems != null) ? m_arrItems.Count : 0;
          }
      }
      public CompressionLevel DefaultCompressionLevel
      {
          get
          {
              return m_defaultLevel;
          }
          set
          {
              m_defaultLevel = value;
          }
      }
      public IFileNamePreprocessor FileNamePreprocessor
      {
          get
          {
              return m_fileNamePreprocessor;
          }
          set
          {
              m_fileNamePreprocessor = value;
          }
      }
      public ZipArchiveItem this[int index]
      {
          get
          {
              if (index < 0 || index > m_arrItems.Count)
                  throw new ArgumentOutOfRangeException("index");

              return (ZipArchiveItem)m_arrItems[index];
          }
      }
      public ZipArchiveItem this[string itemName]
      {
          get
          {
              return m_dicItems[itemName] as ZipArchiveItem;
          }
      }
      public ZipArchiveItem AddDirectory(string directoryName)
      {
          if (string.IsNullOrEmpty(directoryName))
              throw new ArgumentOutOfRangeException("directoryName");

#if !WindowsCE
          FileAttributes attributes = File.GetAttributes(directoryName);
#else
          const FileAttributes attributes = FileAttributes.Directory;
#endif

          if (m_fileNamePreprocessor != null)
          {
              directoryName = m_fileNamePreprocessor.PreprocessName(directoryName);
          }

          return AddItem(directoryName, null, false, attributes);
      }
 
      public ZipArchiveItem AddFile(string fileName)
      {
          Stream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);

#if !WindowsCE
          FileAttributes attributes = File.GetAttributes(fileName);
#else
          const FileAttributes attributes = FileAttributes.Normal;
#endif

          if (m_fileNamePreprocessor != null)
          {
              fileName = m_fileNamePreprocessor.PreprocessName(fileName);
          }

          return AddItem(fileName, stream, true, attributes);
      }


      public ZipArchiveItem AddFile(FileInfo file)
      {
          Stream stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);

#if !WindowsCE
          FileAttributes attributes = File.GetAttributes(file.FullName);
#else
          const FileAttributes attributes = FileAttributes.Normal;
#endif

          if (m_fileNamePreprocessor != null)
          {
              //file.Name = m_fileNamePreprocessor.PreprocessName(file.Name);
          }

          return AddItem(file.Name, stream, true, attributes);
      }

      public ZipArchiveItem AddItem(string itemName, Stream data, bool bControlStream, FileAttributes attributes)
      {
          itemName = itemName.Replace('\\', '/');

          if (itemName.IndexOf(':') >= 0)
              throw new ArgumentOutOfRangeException("ZipItem name contains illegal characters.", "itemName");

          if (m_dicItems.Contains(itemName))
              throw new ArgumentOutOfRangeException("Item " + itemName + " already exists in the archive");

          ZipArchiveItem item = new ZipArchiveItem(itemName, data, bControlStream, attributes);
          item.CompressionLevel = m_defaultLevel;
          return AddItem(item);
      }
    
      public ZipArchiveItem AddItem(ZipArchiveItem item)
      {
          if (item == null)
              throw new ArgumentNullException("item");

          m_arrItems.Add(item);
          m_dicItems.Add(item.ItemName, item);
          return item;
      }
      public void Close()
      {
          for (int i = 0, len = m_arrItems.Count; i < len; i++)
          {
              ZipArchiveItem item = (ZipArchiveItem)m_arrItems[i];
              item.Close();
          }

          m_arrItems.Clear();

          m_dicItems.Clear();
          m_dicItems = null;
      }

      public int Find(Regex itemRegex)
      {
          int iResult = -1;

          for (int i = 0, len = m_arrItems.Count; i < len; i++)
          {
              ZipArchiveItem currentItem = (ZipArchiveItem)m_arrItems[i];
              string strItemName = currentItem.ItemName;

              if (itemRegex.IsMatch(strItemName))
              {
                  iResult = i;
                  break;
              }
          }

          return iResult;
      }

      public int Find(string itemName)
      {
          ZipArchiveItem item = (ZipArchiveItem)m_dicItems[itemName];
          int iResult = -1;

          if (item != null)
          {
              for (int i = 0, len = m_arrItems.Count; i < len; i++)
              {
                  ZipArchiveItem currentItem = (ZipArchiveItem)m_arrItems[i];

                  if (currentItem == item)
                  {
                      iResult = i;
                      break;
                  }
              }
          }

          return iResult;
      }

      public static long FindValueFromEnd(Stream stream, uint value, int maxCount)
      {
          if (stream == null)
              throw new ArgumentNullException("stream");

          if (!stream.CanSeek || !stream.CanRead)
              throw new ArgumentOutOfRangeException("We need to have seekable and readable stream.");

          long lStreamSize = stream.Length;

          if (lStreamSize < 4)
              return -1;

          byte[] arrBuffer = new byte[4];
          long lLastPos = Math.Max(0, lStreamSize - maxCount);
          long lCurrentPosition = lStreamSize - 1 - Constants.IntSize;

          stream.Seek(lCurrentPosition, SeekOrigin.End);
          stream.Read(arrBuffer, 0, Constants.IntSize);
          uint uiCurValue = BitConverter.ToUInt32(arrBuffer, 0);
          bool bFound = (uiCurValue == value);

          if (!bFound)
          {
              while (lCurrentPosition > lLastPos)
              {
                  uiCurValue <<= 8;
                  lCurrentPosition--;
                  stream.Position = lCurrentPosition;
                  uiCurValue += (uint)stream.ReadByte();

                  if (uiCurValue == value)
                  {
                      bFound = true;
                      break;
                  }
              }
          }

          return bFound ? lCurrentPosition : -1;
      }
      public void Open(Stream stream, bool closeStream)
      {
          if (stream == null)
              throw new ArgumentNullException("stream");

          byte[] arrBuffer = new byte[Constants.IntSize];
          long lCentralDirEndPosition = FindValueFromEnd(stream, Constants.CentralDirectoryEndSignature, 65557);

          if (lCentralDirEndPosition < 0)
              throw new ZipException("Can't locate end of central directory record. Possible wrong file format or archive is corrupt.");
          stream.Position = lCentralDirEndPosition + Constants.CentralDirSizeOffset;
          int iCentralDirSize = ReadInt32(stream);

          long lCentralDirPosition = lCentralDirEndPosition - iCentralDirSize;

          stream.Position = lCentralDirPosition;

          ReadCentralDirectoryData(stream);
          ExtractItems(stream);
      }
      
      public void Open(string inputFileName)
      {
          if (inputFileName == null || inputFileName.Length == 0)
              throw new ArgumentOutOfRangeException("inputFileName");

          using (FileStream stream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read))
          {
              Open(stream, false);
          }
      }
      public static short ReadInt16(Stream stream)
      {
          if (stream.Read(s_tempBuffer, 0, Constants.ShortSize) != Constants.ShortSize)
          {
              throw new ZipException("Unable to read value at the specified position - end of stream was reached.");
          }

          return BitConverter.ToInt16(s_tempBuffer, 0);
      }
      public static int ReadInt32(Stream stream)
      {
          if (stream.Read(s_tempBuffer, 0, Constants.IntSize) != Constants.IntSize)
          {
              throw new ZipException("Unable to read value at the specified position - end of stream was reached.");
          }

          return BitConverter.ToInt32(s_tempBuffer, 0);
      }
 
      public void Remove(Regex mask)
      {
          for (int i = 0, len = m_arrItems.Count; i < len; i++)
          {
              ZipArchiveItem item = (ZipArchiveItem)m_arrItems[i];
              string strItemName = item.ItemName;

              if (mask.IsMatch(strItemName))
              {
                  //RemoveAt( i );
                  m_arrItems.RemoveAt(i);
                  m_dicItems.Remove(strItemName);
                  i--;
                  len--;
              }
          }
      }
  
      public void RemoveAt(int index)
      {
          if (index < 0 || index >= m_arrItems.Count)
              throw new ArgumentOutOfRangeException("index");

          ZipArchiveItem item = this[index];
          m_arrItems.RemoveAt(index);
          m_dicItems.Remove(item.ItemName);
      }
   
      public void RemoveItem(string itemName)
      {
          int iItemIndex = Find(itemName);

          if (iItemIndex >= 0)
          {
              RemoveAt(iItemIndex);
          }
      }
      
      public void Save(Stream stream, bool closeStream)
      {
          if (stream == null)
          {
              throw new ArgumentNullException();
          }

          Stream originalStream = null;

          if (!stream.CanSeek)
          {
              originalStream = stream;
              stream = new MemoryStream();
          }

          for (int i = 0, len = m_arrItems.Count; i < len; i++)
          {
              ZipArchiveItem item = (ZipArchiveItem)m_arrItems[i];
              item.Write(stream);
          }

          WriteCentralDirectory(stream);

          if (originalStream != null)
          {
              stream.Position = 0;
              ((MemoryStream)stream).WriteTo(originalStream);
              stream.Close();
              stream = originalStream;
          }

          if (closeStream)
          {
              stream.Close();
          }
      }
      
      public void Save(string outputFileName)
      {
          if (outputFileName == null || outputFileName.Length == 0)
          {
              throw new ArgumentOutOfRangeException("outputFileName");
          }

          Save(outputFileName, false);
      }

      public void Save(string outputFileName, bool createFilePath)
      {
          if (outputFileName == null || outputFileName.Length == 0)
          {
              throw new ArgumentOutOfRangeException("outputFileName");
          }

          if (createFilePath)
          {
              string strPath = Path.GetFullPath(outputFileName);
              string strFolderName = Path.GetDirectoryName(strPath);

              if (!Directory.Exists(strFolderName))
              {
                  Directory.CreateDirectory(strFolderName);
              }
          }

          using (FileStream stream = new FileStream(outputFileName, FileMode.Create, FileAccess.Write))
          {
              Save(stream, false);
          }
      }

      public void UpdateItem(string itemName, byte[] newData)
      {
          ZipArchiveItem item = this[itemName];

          if (item == null)
              throw new ArgumentOutOfRangeException("itemName", "Cannot find specified item.");

          MemoryStream newDataStream = new MemoryStream(newData);
          item.Update(newDataStream, true);
      }
   
      public void UpdateItem(string itemName, Stream newDataStream, bool controlStream)
      {
          ZipArchiveItem item = this[itemName];

          if (item == null)
              throw new ArgumentOutOfRangeException("itemName", "Cannot find specified item.");

          item.Update(newDataStream, controlStream);
      }
    
      public void UpdateItem(string itemName, Stream newDataStream, bool controlStream,
        FileAttributes attributes)
      {
          ZipArchiveItem item = this[itemName];

          if (item != null)
          {
              item.Update(newDataStream, controlStream);
          }
          else
          {
              AddItem(itemName, newDataStream, controlStream, attributes);
          }
      }
      private void ExtractItems(Stream stream)
      {
          if (stream == null)
              throw new ArgumentNullException();

          if (!stream.CanSeek || !stream.CanRead)
              throw new ArgumentOutOfRangeException("stream", "We need seekable and readable stream to parse items.");

          for (int i = 0, len = m_arrItems.Count; i < len; i++)
          {
              ZipArchiveItem item = (ZipArchiveItem)m_arrItems[i];
              item.ReadData(stream, m_bCheckCrc);
              m_dicItems.Add(item.ItemName, item);
          }
      }
      private void ReadCentralDirectoryData(Stream stream)
      {
          if (stream == null)
              throw new ArgumentNullException("stream");

          while (ReadInt32(stream) == Constants.CentralHeaderSignature)
          {
              ZipArchiveItem item = new ZipArchiveItem();
              item.ReadCentralDirectoryData(stream);
              m_arrItems.Add(item);
          }
      }
      private void WriteCentralDirectory(Stream stream)
      {
          long lStartPosition = stream.Position;

          for (int i = 0, len = m_arrItems.Count; i < len; i++)
          {
              ZipArchiveItem item = (ZipArchiveItem)m_arrItems[i];
              item.WriteFileHeader(stream);
          }

          WriteCentralDirectoryEnd(stream, lStartPosition);
      }
      private void WriteCentralDirectoryEnd(Stream stream, long directoryStart)
      {
          if (stream == null)
          {
              throw new ArgumentNullException("stream");
          }

          int iDirectorySize = (int)(stream.Position - directoryStart);
          stream.Write(BitConverter.GetBytes(Constants.CentralDirectoryEndSignature),
            0, Constants.IntSize);

          stream.WriteByte(0);
          stream.WriteByte(0);

          stream.WriteByte(0);
          stream.WriteByte(0);

          byte[] arrData = BitConverter.GetBytes((short)m_arrItems.Count);
          stream.Write(arrData, 0, Constants.ShortSize);

          stream.Write(arrData, 0, Constants.ShortSize);

          stream.Write(BitConverter.GetBytes(iDirectorySize), 0, Constants.IntSize);

          stream.Write(BitConverter.GetBytes((int)directoryStart), 0, Constants.IntSize);

          stream.WriteByte(0);
          stream.WriteByte(0);
      }

#region IDisposable Members
    public void Dispose()
    {
      if( m_arrItems != null )
      {
        for( int i = 0, len = m_arrItems.Count ; i < len ; i++ )
        {
          ZipArchiveItem item = ( ZipArchiveItem )m_arrItems[ i ];
          item.Dispose();
        }

        GC.SuppressFinalize( this );
      }
    }
    ~ZipArchive()
    {
      if( m_arrItems != null )
      {
        Dispose();
      }
    }
#endregion
  }//class
    //class
    //class
}//ns
