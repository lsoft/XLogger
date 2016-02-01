using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using XLogger.Components.Serializer;
using XLogger.Helper;

namespace XLogger.Components.Message.C
{
    /// <summary>
    /// Default log message factory
    /// </summary>
    public class CLogMessageFactory : ILogMessageFactory
    {
        private const string ExceptionString = "--конец трассировки внутреннего стека исключений--";

        private readonly IMessageSerializer _messageSerializer;

        public CLogMessageFactory(
            IMessageSerializer messageSerializer
            )
        {
            if (messageSerializer == null)
            {
                throw new ArgumentNullException("messageSerializer");
            }
            _messageSerializer = messageSerializer;
        }


        /// <summary>
        /// Compose a log message
        /// </summary>
        /// <param name="dateTime">Time label</param>
        /// <param name="category">Message category</param>
        /// <param name="source">Message source</param>
        /// <param name="message">Additional message</param>
        /// <returns>Composed message</returns>
        public ILogMessage CreateMessage(
            DateTime dateTime,
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

            var result = DoCreate(
                dateTime,
                category,
                false,
                false,
                source,
                message,
                string.Empty,
                string.Empty,
                null
                );

            return
                result;
        }

        /// <summary>
        /// Compose an exception message.
        /// </summary>
        /// <param name="dateTime">Time label</param>
        /// <param name="exception">Source exception</param>
        /// <param name="message">Additional message</param>
        /// <returns>Composed exception message</returns>
        public ILogMessage CreateExceptionMessage(
            DateTime dateTime,
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

            var exceptionMessage = DoCreateExceptionMessage(
                dateTime,
                exception
                );

            var stackTrace = new StackTrace(4, true).ToString();

            var result = DoCreate(
                dateTime,
                LogMessageCategoryEnum.Error,
                false,
                false,
                ExceptionString,
                message,
                string.Empty,
                stackTrace,
                exceptionMessage
                );

            return
                result;
        }

        #region private code

        private ILogMessage DoCreateExceptionMessage(
            DateTime dateTime,
            Exception exception
            )
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }

            var innerException = exception.InnerException;

            ILogMessage innerMessage = null;
            if (innerException != null)
            {
                innerMessage = DoCreateExceptionMessage(
                    dateTime,
                    innerException
                    );
            }

            var result = DoCreate(
                dateTime,
                LogMessageCategoryEnum.Error,
                true,
                innerException == null,
#if WindowsCE
                "CFRuntime",
#else
                exception.Source,
#endif
                 exception.GetType().Name + ": " + exception.Message,
                exception.StackTrace,
                string.Empty,
                innerMessage
                );

            return
                result;
        }

        private ILogMessage DoCreate(
            DateTime dateTime,
            LogMessageCategoryEnum messageCategory,
            bool isException,
            bool isRootMessage,
            string strSource,
            string strMessage,
            string strExceptionStackTrace,
            string strMessageStackTrace,
            ILogMessage innerLogMessage
            )
        {
            ILogMessage result = null;

            try
            {
                strExceptionStackTrace = StackNormalize(strExceptionStackTrace, false);
                strMessageStackTrace = StackNormalize(strMessageStackTrace, true);

                result = new CLogMessage(
                    _messageSerializer,

                    dateTime,
                    messageCategory,
                    isException,
                    isRootMessage,
                    strSource,
                    strMessage,
                    strMessageStackTrace,
                    strExceptionStackTrace,
                    innerLogMessage
                    );
            }
            catch
            {
                //no way to log any logger error
                //use fake message in this case

                result = FakeLogMessage;
            }

            return
                result;
        }

        /// <summary>
        /// Нормализация текста трассировки стека для протокола
        /// </summary>
        private static string StackNormalize(string strStack, bool requestLastLineBreak)
        {
            if (strStack != string.Empty)
            {
                strStack = string.Format("\n{0}", strStack);
                strStack = Regex.Replace(strStack, @"[\n]+\z", "", RegexOptions.Singleline);

                if (requestLastLineBreak)
                {
                    strStack = string.Format("{0}\n", strStack);
                }
            }

            return strStack;
        }

        /// <summary>
        /// Fake message. Due to it's stateless nature it's singleton.
        /// </summary>
        private static readonly ILogMessage FakeLogMessage = new FakeLogMessageClass();

        /// <summary>
        /// Fake message. It uses in cases of catastrofic error in composing correct log message.
        /// </summary>
        private class FakeLogMessageClass : ILogMessage
        {
            public string ConvertToString()
            {
                return
                    string.Empty;
            }

            public byte[] ConvertToBinary()
            {
                return 
                    new byte[0];
            }
        }

        #endregion
    }
}