using System;
using System.Text;
using XLogger.Components.Serializer;
using XLogger.Helper;

namespace XLogger.Components.Message.C
{
    /// <summary>
    /// Default log message
    /// </summary>
    public class CLogMessage : ILogMessage
    {
        private readonly IMessageSerializer _messageSerializer;

        private readonly DateTime _timeStamp;
        private readonly LogMessageCategoryEnum _messageCategory;
        private readonly bool _isException;
        private readonly bool _isRootMessage;
        private readonly string _source;
        private readonly string _message;
        private readonly string _messageStackTrace;
        private readonly string _exceptionStackTrace;
        private readonly ILogMessage _childMessage;

        public CLogMessage(
            IMessageSerializer messageSerializer,

            DateTime timeStamp,
            LogMessageCategoryEnum messageCategory,
            bool isException,
            bool isRootMessage,
            string source,
            string message,
            string messageStackTrace,
            string exceptionStackTrace,

            ILogMessage childMessage
            )
        {
            if (messageSerializer == null)
            {
                throw new ArgumentNullException("messageSerializer");
            }
            //childMessage allowed to be null

            _messageSerializer = messageSerializer;

            _timeStamp = timeStamp;
            _messageCategory = messageCategory;
            _isException = isException;
            _isRootMessage = isRootMessage;
            _source = source;
            _message = message.CrLnNormalize();
            _messageStackTrace = messageStackTrace.CrLnNormalize();
            _exceptionStackTrace = exceptionStackTrace.CrLnNormalize();

            _childMessage = childMessage;
        }

        public string ConvertToString()
        {
            var strIsRootMessageFlag = this._isRootMessage ? "root" : string.Empty;
            var strIsExceptionFlag = this._isException ? "Except" : "Message";

//#if WindowsCE
//            const string delimiter = Environment.NewLine;//"\r\n";
//#else
            const string delimiter = "";
//#endif

            var sb = new StringBuilder();

            if (_childMessage != null)
            {
                sb.Append(_childMessage.ConvertToString());
            }

            sb.Append(
                string.Format(
                    "{0}\t{1}\t{2}\t{3}\t{4}\t{5}{6}{7}{8}",
                    // {0} Временная метка
                    this._timeStamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    // {1} Категория
                    this._messageCategory,
                    // {2} Признак исключения
                    strIsExceptionFlag,
                    // {3} Признак корневого исключения
                    strIsRootMessageFlag,
                    // {4} Источник сообщения/исключения
                    this._source,
                    // {5} Сообщение
                    this._message.CrLnNormalize(),
                    // {6} Трассировка стека
                    (string.IsNullOrEmpty(this._messageStackTrace) ? string.Empty : delimiter + this._messageStackTrace).CrLnNormalize(),
                    // {7} Обратная трассировка стека (для исключений)
                    (string.IsNullOrEmpty(this._exceptionStackTrace) ? string.Empty : delimiter + this._exceptionStackTrace).CrLnNormalize(),
                    //{8}
                    Environment.NewLine
                    )
                );

            return
                sb.ToString();
        }

        public byte[] ConvertToBinary()
        {
            var message = this.ConvertToString();

            var bytes = _messageSerializer.Convert(message);

            return
                bytes;
        }

    }
}
