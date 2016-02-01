using XLogger.Components.Message;

namespace XLogger.Logger
{
    /// <summary>
    /// Common logger settings
    /// </summary>
    public interface ICommonMessageSettings
    {
        /// <summary>
        /// Saparator message
        /// </summary>
        ILogMessage SeparatorMessage
        {
            get;
        }

        /// <summary>
        /// Opening log file message
        /// </summary>
        ILogMessage FirstMessage
        {
            get;
        }


        /// <summary>
        /// Closing log file message
        /// </summary>
        ILogMessage LastMessage
        {
            get;
        }
    }
}