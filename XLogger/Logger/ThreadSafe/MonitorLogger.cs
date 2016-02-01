using System;
using XLogger.Helper;

namespace XLogger.Logger.ThreadSafe
{
    /// <summary>
    /// A DECORATOR-logger with a thread safety.
    /// It is using Monitor for a thread safety, so it's optimal on platforms with only 1 CPU logical core for .Net.
    /// </summary>
    public class MonitorLogger : IMessageLogger, IDisposable
    {
        /// <summary>
        /// Internal syncronization object.
        /// </summary>
        private readonly object _locker = new object();

        /// <summary>
        /// Internal logger.
        /// </summary>
        private readonly IMessageLogger _logger;

        /// <summary>
        /// Dispose marker.
        /// </summary>
        private volatile bool _disposed = false;

        public MonitorLogger(
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

            lock(_locker)
            {
                _logger.LogException(
                    exception,
                    message
                    );
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
            
            lock(_locker)
            {
                _logger.LogCategorizedMessage(
                    category,
                    source,
                    message
                    );
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
            lock (_locker)
            {
                if (!_disposed)
                {
                    _disposed = true;

                    //nothing to do
                }
            }
        }
    }
}
