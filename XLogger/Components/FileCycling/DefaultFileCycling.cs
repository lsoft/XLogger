using System;
using System.Diagnostics;
using XLogger.Components.FileWrapper;
using XLogger.Components.Message;
using XLogger.Components.OpenReasoning;
using XLogger.Helper;
using XLogger.Logger.File;

namespace XLogger.Components.FileCycling
{
    /// <summary>
    /// Default file-writer with a file-cycling functionality.
    /// </summary>
    public class DefaultFileCycling : IFileCycling
    {
        /// <summary>
        /// Log files settings.
        /// </summary>
        private readonly ILogFilesSettings _logFilesSettings;

        /// <summary>
        /// Need to open a new log file worker.
        /// </summary>
        private readonly IOpenReasoning _openReasoning;

        /// <summary>
        /// Current log file.
        /// </summary>
        private volatile ILogFileWrapper _current;

        /// <summary>
        /// Dispose marker.
        /// </summary>
        private volatile bool _disposed = false;



        public DefaultFileCycling(
            ILogFilesSettings logFilesSettings,
            IOpenReasoning openReasoning
            )
        {
            if (logFilesSettings == null)
            {
                throw new ArgumentNullException("logFilesSettings");
            }
            if (openReasoning == null)
            {
                throw new ArgumentNullException("openReasoning");
            }

            _logFilesSettings = logFilesSettings;
            _openReasoning = openReasoning;

            DeleteOldFiles();

            //using last existing log file
            _current = _logFilesSettings.FileProvider.GetLastFile();

            Debug.WriteLine("choosed: " + (_current != null ? _current.FileName : "no file"));
        }

        public void WriteMessage(
            ILogMessage message
            )
        {
            if (_disposed)
            {
                return;
            }
            if (message == null)
            {
                return;
            }

            PrepareFileForWrite();

            _current.WriteMessage(message);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                SafelyCloseFileStream(LogFileCloseReasonEnum.Other);
            }
        }

        #region private code

        private void PrepareFileForWrite()
        {
            var needToOpenNewFile = _openReasoning.IsNeedToOpenNewFile(
                _current
                );

            if (needToOpenNewFile)
            {
                SafelyCloseFileStream(LogFileCloseReasonEnum.Obsolete);

                OpenNextFileStream();
                DeleteOldFiles();
            }
        }


        private void SafelyCloseFileStream(LogFileCloseReasonEnum reason)
        {
            if (_current != null && _current.IsActive)
            {
                Debug.WriteLine("close: " + _current.FileName);

                _current.Close(reason);
                _current = null;
            }
        }

        private void OpenNextFileStream(
            )
        {
            if (_current != null && _current.IsActive)
            {
                //current file is not closed
                //so cancel to open next file

                return;
            }

            _current = _logFilesSettings.FileProvider.CreateNextFile();
        }

        private void DeleteOldFiles()
        {
            _logFilesSettings.FileProvider.DeleteOldFiles(_logFilesSettings.MaxFileCount);
        }



        #endregion
    }
}