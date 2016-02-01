namespace XLogger.Components.Serializer
{
    /// <summary>
    /// Message serializer.
    /// </summary>
    public interface IMessageSerializer
    {
        byte[] Convert(
            string message
            );
    }
}