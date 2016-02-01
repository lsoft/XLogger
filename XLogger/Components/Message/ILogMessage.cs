
namespace XLogger.Components.Message
{
    /// <summary>
    /// Сообщение логгера
    /// </summary>
    public interface ILogMessage
    {
        string ConvertToString();

        byte[] ConvertToBinary();
    }
}