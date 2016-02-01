using System;
using XLogger.Components.Serializer;
using XLogger.Helper;

namespace XLogger.Components.Message.Service
{
    /// <summary>
    /// Service log message (search for separator, first and last messages).
    /// </summary>
    internal class ServiceLogMessage : ILogMessage
    {
        private readonly IMessageSerializer _messageSerializer;

        private readonly string _message;
        private readonly string _source;

        public ServiceLogMessage(
            IMessageSerializer messageSerializer,
            
            string source,
            string message
            )
        {
            if (messageSerializer == null)
            {
                throw new ArgumentNullException("messageSerializer");
            }

            _messageSerializer = messageSerializer;
            
            _source = source;
            _message = message.CrLnNormalize();
        }

        public string ConvertToString()
        {
            var now = AmbientContext.Now;

            var result = string.Format(
                "{0}\t{1}\t\t\t{2}\t{3}",
                // {0} Временная метка
                now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                // {1} Категория
                LogMessageCategoryEnum.Info,
                // {4} Источник сообщения/исключения
                this._source,
                // {5} Сообщение
                this._message.CrLnNormalize()
                );

            return
                result;
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
