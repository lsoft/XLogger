using XLogger.Components.FileProvider;

namespace XLogger.Logger.File
{
    /// <summary>
    /// ��������� ���-������
    /// </summary>
    public interface ILogFilesSettings
    {
        /// <summary>
        /// �����, ��� �������� ����
        /// </summary>
        string LogFolder
        {
            get;
        }

        /// <summary>
        /// ����������� ������
        /// </summary>
        IFileProvider FileProvider
        {
            get;
        }


        /// <summary>
        /// ������������ ������ ����� ���������
        /// </summary>
        int MaxFileCount
        {
            get;
        }

        /// <summary>
        /// ������������ ���-�� ������ ���������
        /// </summary>
        int MaxFileSize
        {
            get;
        }

    }
}