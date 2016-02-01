using System;

namespace XLogger.Components.FileWrapper
{
    /// <summary>
    /// Factory of log file wrappers.
    /// </summary>
    public interface ILogFileWrapperFactory
    {
        ILogFileWrapper CreateLogFileWrapper(
            DateTime createDate,
            string filePath,
            LogFileOpenModeEnum fileOpenMode
            );
    }
}