
namespace XLogger.Components.Message
{
    /// <summary>
    /// ��������� �������
    /// </summary>
    public interface ILogMessage
    {
        string ConvertToString();

        byte[] ConvertToBinary();
    }
}