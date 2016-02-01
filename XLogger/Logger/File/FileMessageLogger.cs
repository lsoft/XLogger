using System;
using System.Diagnostics;
using XLogger.Components.FileCycling;
using XLogger.Components.Message;
using XLogger.Helper;

namespace XLogger.Logger.File
{
    /// <summary>
    /// File-writer logger.
    /// </summary>
    public class FileMessageLogger : IMessageLogger, IDisposable
    {
        /// <summary>
        /// File-writer with a file-cycling functionality.
        /// </summary>
        private readonly IFileCycling _fileCycling;

        /// <summary>
        /// Factory of logger messages.
        /// </summary>
        private readonly ILogMessageFactory _logMessageFactory;

        /// <summary>
        /// Dispose marker.
        /// </summary>
        private volatile bool _disposed = false;

        public FileMessageLogger(
            IFileCycling fileCycling,
            ILogMessageFactory logMessageFactory
            )
        {
            if (fileCycling == null)
            {
                throw new ArgumentNullException("fileCycling");
            }
            if (logMessageFactory == null)
            {
                throw new ArgumentNullException("logMessageFactory");
            }

            _fileCycling = fileCycling;
            _logMessageFactory = logMessageFactory;
        }

        /// <summary>
        /// Write exception message to logger.
        /// </summary>
        public void LogException(Exception exception)
        {
            this.LogException(exception, string.Empty);
        }

        /// <summary>
        ///  Write exception message to logger.
        /// </summary>
        public void LogException(
            Exception exception,
            string message
            )
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (_disposed)
            {
                //skip a logging work when logger has disposed

                return;
            }

            var dateTime = AmbientContext.Now;

            var logMessage = _logMessageFactory.CreateExceptionMessage(
                dateTime,
                exception,
                message
                );

            _fileCycling.WriteMessage(
                logMessage
                );
        }

        /// <summary>
        /// Write categorized message to logger.
        /// (it's easier to use LogMessage + LogWarning methods instead).
        /// </summary>
        public void LogCategorizedMessage(
            LogMessageCategoryEnum category,
            string source,
            string message
            )
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (_disposed)
            {
                //skip a logging work when logger has disposed

                return;
            }

            var dateTime = AmbientContext.Now;

            var logMessage = _logMessageFactory.CreateMessage(
                dateTime,
                category,
                source,
                message
                );

            _fileCycling.WriteMessage(
                logMessage
                );
        }

        /// <summary>
        /// Write message to logger.
        /// </summary>
        public void LogMessage(
            string message
            )
        {
            var source = StackHelper.GetClassNameFromStack();

            this.LogCategorizedMessage(
                LogMessageCategoryEnum.Info,
                source,
                message
                );
        }

        /// <summary>
        /// Write message to logger.
        /// </summary>
        public void LogWarning(
            string message
            )
        {
            var source = StackHelper.GetClassNameFromStack();

            this.LogCategorizedMessage(
                LogMessageCategoryEnum.Warning,
                source,
                message
                );
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                _fileCycling.Dispose();
            }
        }

    }
}