using System;
using XLogger.Components.FileProvider;

namespace XLogger.Logger.File
{
    public class LogFilesSettings : ILogFilesSettings
    {
        /// <summary>
        /// Папка, где хранятся логи
        /// </summary>
        public string LogFolder
        {
            get;
            private set;
        }

        /// <summary>
        /// Именователь файлов
        /// </summary>
        public IFileProvider FileProvider
        {
            get;
            private set;
        }

        /// <summary>
        /// Максимальный размер файла протокола
        /// </summary>
        public int MaxFileCount
        {
            get;
            private set;
        }

        /// <summary>
        /// Максимальное кол-во файлов протокола
        /// </summary>
        public int MaxFileSize
        {
            get;
            private set;
        }


        public LogFilesSettings(
            string logFolder,
            IFileProvider fileProvider,
            int maxFileCount,
            int maxFileSize
            )
        {
            if (logFolder == null)
            {
                throw new ArgumentNullException("logFolder");
            }
            if (fileProvider == null)
            {
                throw new ArgumentNullException("fileProvider");
            }


            LogFolder = logFolder;
            FileProvider = fileProvider;
            MaxFileCount = maxFileCount;
            MaxFileSize = maxFileSize;
        }
    }
}