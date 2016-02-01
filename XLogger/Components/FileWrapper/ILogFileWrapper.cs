using System;
using System.IO;
using XLogger.Helper;

namespace XLogger.Components.FileWrapper
{
    /// <summary>
    /// Log file wrapper.
    /// </summary>
    public interface ILogFileWrapper : IWriteable, IDisposable
    {
        /// <summary>
        /// Only file name without a folder name.
        /// </summary>
        string FileName
        {
            get;
        }

        /// <summary>
        /// Create date.
        /// </summary>
        DateTime CreateDate
        {
            get;
        }

        /// <summary>
        /// Current size of the file.
        /// </summary>
        long FileSize
        {
            get;
        }

        /// <summary>
        /// Is this file active?
        /// (Active file allowed to append new messages).
        /// </summary>
        bool IsActive
        {
            get;
        }

        /// <summary>
        /// Delete the file from a file system.
        /// </summary>
        void Delete();

        /// <summary>
        /// Close the log file and mark this file as inactive.
        /// </summary>
        /// <param name="reason">Reason for closing.</param>
        void Close(LogFileCloseReasonEnum reason);
    }
}