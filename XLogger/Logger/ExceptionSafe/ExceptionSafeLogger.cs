using System;
using XLogger.Helper;

namespace XLogger.Logger.ExceptionSafe
{
    /// <summary>
    /// A decorator logger with exception suppression.
    /// </summary>
    public class ExceptionSafeLogger : IMessageLogger, IDisposable
    {
        private readonly IMessageLogger _logger;

        private volatile bool _disposed = false;

        public ExceptionSafeLogger(
            IMessageLogger logger
            )
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            _logger = logger;
        }

        /// <summary>
        /// Записать в протокол сообщение, вызванное исключением
        /// </summary>
        public void LogException(Exception exception)
        {
            this.LogException(exception, string.Empty);
        }

        /// <summary>
        /// Записать в протокол сообщение, вызванное исключением
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

            try
            {
                _logger.LogException(
                    exception,
                    message
                    );
            }
            catch
            {
                //no way to log any logger error
                //nothing can be done here
                //force this error to be skipped
            }
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
            
            try
            {
                _logger.LogCategorizedMessage(
                    category,
                    source,
                    message
                    );
            }
            catch
            {
                //no way to log any logger error
                //nothing can be done here
                //force this error to be skipped
            }
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

                //nothing to do
            }
        }
    }
}
