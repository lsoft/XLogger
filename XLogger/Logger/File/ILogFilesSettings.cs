using XLogger.Components.FileProvider;

namespace XLogger.Logger.File
{
    /// <summary>
    /// Настройки лог-файлов
    /// </summary>
    public interface ILogFilesSettings
    {
        /// <summary>
        /// Папка, где хранятся логи
        /// </summary>
        string LogFolder
        {
            get;
        }

        /// <summary>
        /// Именователь файлов
        /// </summary>
        IFileProvider FileProvider
        {
            get;
        }


        /// <summary>
        /// Максимальный размер файла протокола
        /// </summary>
        int MaxFileCount
        {
            get;
        }

        /// <summary>
        /// Максимальное кол-во файлов протокола
        /// </summary>
        int MaxFileSize
        {
            get;
        }

    }
}