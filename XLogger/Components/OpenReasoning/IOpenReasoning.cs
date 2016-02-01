using XLogger.Components.FileWrapper;

namespace XLogger.Components.OpenReasoning
{
    /// <summary>
    /// Need to open a new log file worker.
    /// </summary>
    public interface IOpenReasoning
    {
        bool IsNeedToOpenNewFile(
            ILogFileWrapper current
            );
    }
}