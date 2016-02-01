using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using XLogger.Components.Message;
using XLogger.Helper;
using XLogger.Logger;

namespace XLogger.Components.FileWrapper.Text
{
    /// <summary>
    /// Text log file wrapper.
    /// </summary>
    internal class TextLogFileWrapper : ILogFileWrapper
    {
        private readonly ICommonMessageSettings _commonMessageSettings;

        private readonly FileInfo _fileInformation;

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

        public TextLogFileWrapper(
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
            DoClose();

            try
            {
                this._fileInformation.Delete();
            }
            catch
            {
                //no options to do here: no ways to log any logger's errors
                //force this error to be skipped and hoping for better
            }
        }

        public void Close(LogFileCloseReasonEnum reason)
        {
            DoClose();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                DoClose();

                _disposed = true;
            }
        }

        private void DoClose()
        {
            if (IsActive)
            {
                //write tails messages
                WriteTail();

                //flushing data to disk
                _fileStream.Flush();

                //closing the file steeam
                _fileStream.Close();
                _fileStream = null;
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
