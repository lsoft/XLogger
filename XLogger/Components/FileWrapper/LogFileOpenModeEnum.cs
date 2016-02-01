namespace XLogger.Components.FileWrapper
{
    /// <summary>
    /// Log file open mode.
    /// </summary>
    public enum LogFileOpenModeEnum
    {
        /// <summary>
        /// Neither create file actually, nor open file stream actually.
        /// Only create a file wrapper.
        /// </summary>
        NoCreate,

        /// <summary>
        /// Create a new log file, and open file stream for that.
        /// </summary>
        CreateNew,

        /// <summary>
        /// Open existing log file for writing a new data.
        /// </summary>
        AppendToExisting
    }
}