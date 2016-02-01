using System;
using XLogger.Helper;

namespace XLogger.Components.Message
{
    /// <summary>
    /// Log message factory
    /// </summary>
    public interface ILogMessageFactory
    {
        /// <summary>
        /// Compose a log message
        /// </summary>
        /// <param name="dateTime">Time label</param>
        /// <param name="category">Message category</param>
        /// <param name="source">Message source</param>
        /// <param name="message">Additional message</param>
        /// <returns>Composed message</returns>
        ILogMessage CreateMessage(
            DateTime dateTime,
            LogMessageCategoryEnum category,
            string source,
            string message
            );


        /// <summary>
        /// Compose an exception message.
        /// </summary>
        /// <param name="dateTime">Time label</param>
        /// <param name="exception">Source exception</param>
        /// <param name="message">Additional message</param>
        /// <returns>Composed exception message</returns>
        ILogMessage CreateExceptionMessage(
            DateTime dateTime,
            Exception exception,
            string message
            );
    }
}