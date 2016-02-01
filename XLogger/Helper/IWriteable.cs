using XLogger.Components;
using XLogger.Components.Message;

namespace XLogger.Helper
{
    /// <summary>
    /// Thing that can receive log messages.
    /// </summary>
    public interface IWriteable
    {
        void WriteMessage(
            ILogMessage message
            );
    }
}