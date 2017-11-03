using System;
using XLogger.Helper;

namespace XLogger.Logger
{
    public interface IMessageLogger2 : IMessageLogger, IDisposable
    {
        
    }

    public interface IMessageLogger
    {
        /// <summary>
        /// Write exception message to logger.
        /// </summary>
        void LogException(
            Exception exception
            );

        /// <summary>
        ///  Write exception message to logger.
        /// </summary>
        void LogException(
            Exception exception,
            string message
            );


        /// <summary>
        /// Write categorized message to logger.
        /// (it's easier to use LogMessage + LogWarning methods instead).
        /// </summary>
        void LogCategorizedMessage(
            LogMessageCategoryEnum category,
            string source,
            string message
            );

        /// <summary>
        /// Write message to logger.
        /// </summary>
        void LogMessage(
            string message
            );

        /// <summary>
        /// Write message to logger.
        /// </summary>
        void LogWarning(
            string message
            );
    }
}