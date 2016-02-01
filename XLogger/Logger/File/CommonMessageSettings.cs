using System;
using XLogger.Components.Message;
using XLogger.Components.Message.Service;
using XLogger.Components.Serializer;

namespace XLogger.Logger.File
{
    /// <summary>
    /// Common logger settings
    /// </summary>
    public class CommonMessageSettings : ICommonMessageSettings
    {
        /// <summary>
        /// Saparator message
        /// </summary>
        public ILogMessage SeparatorMessage
        {
            get;
            private set;
        }

        /// <summary>
        /// Opening log file message
        /// </summary>
        public ILogMessage FirstMessage
        {
            get;
            private set;
        }

        /// <summary>
        /// Closing log file message
        /// </summary>
        public ILogMessage LastMessage
        {
            get;
            private set;
        }

        public CommonMessageSettings(
            IMessageSerializer messageSerializer,
            string separatorMessage,
            string firstMessage,
            string lastMessage
            )
        {
            if (messageSerializer == null)
            {
                throw new ArgumentNullException("messageSerializer");
            }
            if (separatorMessage == null)
            {
                throw new ArgumentNullException("separatorMessage");
            }
            if (firstMessage == null)
            {
                throw new ArgumentNullException("firstMessage");
            }
            if (lastMessage == null)
            {
                throw new ArgumentNullException("lastMessage");
            }
            if (!separatorMessage.EndsWith(Environment.NewLine))
            {
                separatorMessage += Environment.NewLine;
            }
            if (!firstMessage.EndsWith(Environment.NewLine))
            {
                firstMessage += Environment.NewLine;
            }
            if (!lastMessage.EndsWith(Environment.NewLine))
            {
                lastMessage += Environment.NewLine;
            }

            SeparatorMessage = new ServiceLogMessage(messageSerializer, string.Empty, separatorMessage);
            FirstMessage = new ServiceLogMessage(messageSerializer, string.Empty, firstMessage);
            LastMessage = new ServiceLogMessage(messageSerializer, string.Empty, lastMessage);
        }
    }
}