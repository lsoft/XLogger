using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using XLogger.Components.Message;
using XLogger.Components.Zip.ZIP;
using XLogger.Helper;
using XLogger.Logger;

namespace XLogger.Components.FileWrapper.Zip
{
    /// <summary>
    /// Log file wrapper with zipping closed log files.
    /// </summary>
    internal class ZipLogFileWrapper : ILogFileWrapper
    {
        private readonly ICommonMessageSettings _commonMessageSettings;

        private FileInfo _fileInformation;

        private bool _disposed = false;

        /// <summary>
        /// Current log file stream.
        /// </summary>
        private FileStream _fileStream;

        public string FileName
        {
            get
            {
                return
                    _fileInformation.Name;
            }
        }

        public DateTime CreateDate
        {
            get;
            private set;
        }

        public long FileSize
        {
            get
            {
                return
                    this._fileStream.Position;
            }
        }

        public bool IsActive
        {
            get
            {
                var fs = _fileStream;

                return
                    fs != null && fs.CanWrite;
            }
        }

        public ZipLogFileWrapper(
            ICommonMessageSettings commonMessageSettings,
            DateTime createDate,
            string filePath,
            LogFileOpenModeEnum fileOpenMode
            )
        {
            if (commonMessageSettings == null)
            {
                throw new ArgumentNullException("commonMessageSettings");
            }
            if (filePath == null)
            {
                throw new ArgumentNullException("filePath");
            }

            _commonMessageSettings = commonMessageSettings;

            CreateDate = createDate;

            _fileInformation = new FileInfo(filePath);

            switch (fileOpenMode)
            {
                case LogFileOpenModeEnum.NoCreate:
                    //nothing to do
                    break;
                case LogFileOpenModeEnum.CreateNew:
                    _fileStream = new FileStream(
                        filePath,
                        FileMode.CreateNew,
                        FileAccess.ReadWrite,
                        FileShare.Read
                        );

                    //write head messages after creating new log file
                    WriteHead();
                    break;
                case LogFileOpenModeEnum.AppendToExisting:
                    _fileStream = new FileStream(
                        filePath,
                        FileMode.Open,
                        FileAccess.ReadWrite,
                        FileShare.Read
                        );

                    _fileStream.Position = _fileStream.Length;

                    //write head messages after opening existing log file
                    WriteHead();
                    break;
                default:
                    throw new ArgumentOutOfRangeException("fileOpenMode");
            }
        }

        public void WriteMessage(
            ILogMessage message
            )
        {
            if (message == null)
            {
                return;
            }

            var bytes = message.ConvertToBinary();

            _fileStream.Write(bytes, 0, bytes.Length);
        }

        public void Delete()
        {
            DoClose(false);

            try
            {
                this._fileInformation.Delete();
            }
            catch
            {
                //no options to do here: no ways to log any logger's errors 
            }
        }

        public void Close(LogFileCloseReasonEnum reason)
        {
            DoClose(reason == LogFileCloseReasonEnum.Obsolete);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                DoClose(false);

                _disposed = true;
            }
        }

        private void DoClose(bool needToZip)
        {
            if (IsActive)
            {
                //write tails messages
                WriteTail();

                //flushing data to disk
                _fileStream.Flush();

                var zipFileName = this._fileInformation.FullName + ".zip";

                try
                {
                    if (needToZip)
                    {
                        //жмем
                        CreateZipFileByTextFile(zipFileName);
                    }
                }
                finally
                {
                    _fileStream.Close();
                    _fileStream = null;
                }

                //in case of zipping we must delete unzipped file and replace the file information to zipped copy
                if (needToZip)
                {
                    try
                    {
                        this._fileInformation.Delete();
                        this._fileInformation = new FileInfo(zipFileName);
                    }
                    catch
                    {
                        //no options to do here: no ways to log any logger's errors 
                    }
                }
            }
        }

        private void CreateZipFileByTextFile(
            string zipFileName
            )
        {
            _fileStream.Position = 0;

            //doing zip
            using (var zip = new ZipArchive())
            {
                using (var mstream = new MemoryStream())
                {
                    var buffer = new byte[32768];

                    int read;
                    while ((read = _fileStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        mstream.Write(buffer, 0, read);
                    }

                    var zipItem = new ZipArchiveItem(
                        this._fileInformation.Name,
                        mstream,
                        true,
                        FileAttributes.Normal);

                    zip.AddItem(zipItem);

                    zip.Save(zipFileName);
                }
            }
        }

        private void WriteHead()
        {
            WriteMessage(_commonMessageSettings.SeparatorMessage);
            WriteMessage(_commonMessageSettings.FirstMessage);
        }

        private void WriteTail()
        {
            WriteMessage(_commonMessageSettings.LastMessage);
            WriteMessage(_commonMessageSettings.SeparatorMessage);
        }
    }
}
