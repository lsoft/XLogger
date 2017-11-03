using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XLogger.Components.Message;
using XLogger.Helper;

namespace XLogger.Logger.ExternalAction
{
    /// <summary>
    /// Logger that transfer messages into an external action.
    /// For example, it's useful for a transferring messages into console.
    /// </summary>
    public class ExternalActionLogger : IMessageLogger2
    {
        /// <summary>
        /// External action to process messages.
        /// </summary>
        private readonly Action<string> _writeAction;

        /// <summary>
        /// Common logger settings.
        /// </summary>
        private readonly ICommonMessageSettings _commonMessageSettings;

        /// <summary>
        /// Factory of logger messages.
        /// </summary>
        private readonly ILogMessageFactory _logMessageFactory;
        
        /// <summary>
        /// Dispose marker.
        /// </summary>
        private volatile bool _disposed = false;

        public ExternalActionLogger(
            Action<string> writeAction,
            ICommonMessageSettings commonMessageSettings,
            ILogMessageFactory logMessageFactory
            )
        {
            if (writeAction == null)
            {
                throw new ArgumentNullException("writeAction");
            }
            if (commonMessageSettings == null)
            {
                throw new ArgumentNullException("commonMessageSettings");
            }
            if (logMessageFactory == null)
            {
                throw new ArgumentNullException("logMessageFactory");
            }

            _writeAction = writeAction;
            _commonMessageSettings = commonMessageSettings;
            _logMessageFactory = logMessageFactory;

            WriteHead();
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

            var writeMessage = logMessage.ConvertToString();

            _writeAction(
                writeMessage
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

            var writeMessage = logMessage.ConvertToString();

            _writeAction(
                writeMessage
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

                WriteTail();
            }
        }

        private void WriteHead()
        {
            _writeAction(_commonMessageSettings.SeparatorMessage.ConvertToString());
            _writeAction(_commonMessageSettings.FirstMessage.ConvertToString());
        }

        private void WriteTail()
        {
            _writeAction(_commonMessageSettings.LastMessage.ConvertToString());
            _writeAction(_commonMessageSettings.SeparatorMessage.ConvertToString());
        }

    }
}
