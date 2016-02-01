using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace XLogger.Helper
{
    /// <summary>
    /// Stack-related helper.
    /// </summary>
    internal static class StackHelper
    {
        public static string GetClassNameFromStack()
        {
            var result = "N/A";

#if !WindowsCE
            try
            {
                var stack = new StackTrace(0, true);

                result = stack.GetFrame(2).GetMethod().DeclaringType.Name;
            }
            catch
            {
                //nothing can be done here: it's logger, no way to log any logger error
            }
#endif

            return
                result;
        }
    }
}
