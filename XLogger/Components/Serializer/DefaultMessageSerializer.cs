using System;
using System.Text;

namespace XLogger.Components.Serializer
{
    /// <summary>
    /// Default message serializer.
    /// </summary>
    public class DefaultMessageSerializer : IMessageSerializer
    {
        private readonly Encoding _encoding;

        public DefaultMessageSerializer(
            
            )
        {
            _encoding = Encoding.GetEncoding(1251);
        }

        public DefaultMessageSerializer(
            Encoding encoding
            )
        {
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }
            _encoding = encoding;
        }

        public byte[] Convert(
            string message
            )
        {
            if (message == null)
            {
                return 
                    new byte[0];
            }

            return
                _encoding.GetBytes(message);
        }
    }

}
