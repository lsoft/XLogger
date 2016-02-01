using System;
using XLogger.Helper;

namespace XLogger.Logger.ThreadSafe
{
    /// <summary>
    /// A DECORATOR-logger with a thread safety.
    /// </summary>
    public class ThreadSafeLogger : IMessageLogger, IDisposable
    {
        /// <summary>
        /// Internal logger.
        /// </summary>
        private readonly IMessageLogger _logger;

        /// <summary>
        /// Dispose marker.
        /// </summary>
        private volatile bool _disposed = false;

        public ThreadSafeLogger(
            IMessageLogger logger
            )
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            //choosing a concrete implementation of a thread safety logger
#if !WindowsCE
            if (Environment.ProcessorCount > 1)
            {
                _logger = new BackgroundThreadLogger(logger);
            }
            else
            {
                _logger = new MonitorLogger(logger);
            }
#else
            _logger = new MonitorLogger(logger);
#endif
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

            _logger.LogException(
                exception,
                message
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
            
            _logger.LogCategorizedMessage(
                category,
                source,
                message
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

                //nothing to do
            }
        }
    }
}
