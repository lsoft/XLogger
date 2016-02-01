using XLogger.Components.FileWrapper;

namespace XLogger.Components.FileProvider
{
    /// <summary>
    /// Log file provider.
    /// </summary>
    public interface IFileProvider
    {
        /// <summary>
        /// Create next log file.
        /// </summary>
        ILogFileWrapper CreateNextFile();

        /// <summary>
        /// Get last existing log file.
        /// </summary>
        ILogFileWrapper GetLastFile();
        
        /// <summary>
        /// Delete old log files.
        /// </summary>
        void DeleteOldFiles(int maxFileCount);
    }
}