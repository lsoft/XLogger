namespace XLogger.Helper
{
    /// <summary>
    /// String-related extensions.
    /// </summary>
    internal static class StringExtensions
    {

        public static string CrLnNormalize(this string source)
        {
            return
                source.Replace("\r\n", "\n").Replace("\n\r", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
        }
    }
}