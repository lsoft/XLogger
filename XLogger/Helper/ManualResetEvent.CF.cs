using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace XLogger.Helper
{
    /// <summary>
    /// P/invoke implementation of manual reset event.
    /// </summary>
    internal class ManualResetEvent : NoNamedEvent
    {
        public ManualResetEvent(
            bool initialState
            )
            : base(true, initialState)
        {
        }
    }
}
