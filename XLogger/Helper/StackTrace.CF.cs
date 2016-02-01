using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace XLogger.Helper
{
    /// <summary>
    /// Fake implementation of StackTrace.
    /// </summary>
    internal class StackTrace
    {
        private readonly string _stackTrace;


        public StackTrace(
            int skipFrames,
            bool fNeedFileInfo
            )
        {
            try
            {
                throw new Exception();
            }
            catch (Exception excp)
            {
                _stackTrace = excp.StackTrace;
            }
        }

        public override string ToString()
        {
            return
                _stackTrace;
        }
    }
}
