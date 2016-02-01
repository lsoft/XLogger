using System;
using XLogger.Components.FileProvider;

namespace XLogger.Logger.File
{
    public class LogFilesSettings : ILogFilesSettings
    {
        /// <summary>
        /// �����, ��� �������� ����
        /// </summary>
        public string LogFolder
        {
            get;
            private set;
        }

        /// <summary>
        /// ����������� ������
        /// </summary>
        public IFileProvider FileProvider
        {
            get;
            private set;
        }

        /// <summary>
        /// ������������ ������ ����� ���������
        /// </summary>
        public int MaxFileCount
        {
            get;
            private set;
        }

        /// <summary>
        /// ������������ ���-�� ������ ���������
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