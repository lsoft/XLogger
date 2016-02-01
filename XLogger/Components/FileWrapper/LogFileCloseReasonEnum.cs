namespace XLogger.Components.FileWrapper
{
    /// <summary>
    /// Close log file reason.
    /// </summary>
    public enum LogFileCloseReasonEnum
    {
        /// <summary>
        /// File is obsolete (by any reason) and there will new active log file.
        /// </summary>
        Obsolete,

        /// <summary>
        /// Any other reason (for example, the application is shutting down).
        /// </summary>
        Other
    }
}