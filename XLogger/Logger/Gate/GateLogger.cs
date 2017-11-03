using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XLogger.Components.Message;
using XLogger.Helper;

namespace XLogger.Logger.Gate
{
    /// <summary>
    /// Logger that pushes messages into 1 or more other loggers.
    /// For example, useful in scenarios when logging must be performed to a console and a file.
    /// </summary>
    public class GateLogger : IMessageLogger2
    {
        private readonly IMessageLogger[] _loggers;

        private volatile bool _disposed = false;

        public GateLogger(
            params IMessageLogger[] loggers
            )
        {
            if (loggers == null)
            {
                throw new ArgumentNullException("loggers");
            }
            if (loggers.Length == 0)
            {
                throw new ArgumentException("loggers.Length == 0");
            }

            _loggers = loggers;
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
                //в случае, если логгер задиспозен, просто игнорируем новые сообщения не выбрасывая исключений

                return;
            }

            foreach (var logger in _loggers)
            {
                try
                {
                    logger.LogException(
                        exception,
                        message
                        );
                }
                catch
                {
                    //no way to log any logger error
                    //nothing can be done here

                    //force this error to be skipped
                    //and log message into others loggers
                }
            }
        }



        /// <summary>
        /// Записать сообщение в протокол
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
                //в случае, если логгер задиспозен, просто игнорируем новые сообщения не выбрасывая исключений

                return;
            }

            foreach (var logger in _loggers)
            {
                try
                {
                    logger.LogCategorizedMessage(
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
                    //and log message into others loggers
                }
            }
        }

        /// <summary>
        /// Записать сообщение в протокол
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <returns>Признак успешности записи сообщения</returns>
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
        /// Записать предупреждение в протокол
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <returns>Признак успешности записи сообщения</returns>
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
