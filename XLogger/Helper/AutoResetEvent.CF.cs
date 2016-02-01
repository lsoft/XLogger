namespace XLogger.Helper
{
    /// <summary>
    /// P/invoke implementation of auto reset event.
    /// </summary>
    internal class AutoResetEvent : NoNamedEvent
    {
        public AutoResetEvent(
            bool initialState
            )
            : base(false, initialState)
        {
        }
    }
}