using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace XLogger.Helper
{
    /// <summary>
    /// Wait handle related helper. It implements for a WaitAny method.
    /// </summary>
    internal static class WaitHandleHelper
    {
        public const int WaitTimeout = 0x102;

        public static void WaitAll(NoNamedEvent[] waiters)
        {
            WaitAll(waiters, -1);
        }

        public static void WaitAll(
            NoNamedEvent[] waiters,
            int millisecondTimeout
            )
        {
            throw new NotSupportedException("WindowsCE does not support waiting for ALL events but only for ANY event of the given set. For details please refer https://msdn.microsoft.com/en-us/library/aa450987.aspx");
        }

        public static int WaitAny(NoNamedEvent[] waiters)
        {
            return
                WaitAny(
                    waiters,
                    -1
                    );
        }

        public static int WaitAny(
            NoNamedEvent[] waiters,
            int millisecondTimeout
            )
        {
            if (waiters == null)
            {
                throw new ArgumentNullException("waiters");
            }
            if (waiters.Length == 0)
            {
                throw new ArgumentException("waiters.Length == 0");
            }

            var handlers = GetHandleArray(waiters);

            var index = WaitAnyHandler(millisecondTimeout, handlers);

            return index;
        }

        private static int WaitAnyHandler(
            int millisecondTimeout,
            IntPtr[] handlers
            )
        {
            var index = WaitForMultipleObjects(
                handlers.Length,
                handlers,
                false,
                millisecondTimeout
                );

            return index;
        }

        private static IntPtr[] GetHandleArray(NoNamedEvent[] waiters)
        {
            var handlers = new IntPtr[waiters.Length];

            for (var i = 0; i < waiters.Length; i++)
            {
                handlers[i] = waiters[i].Handle;
            }

            return handlers;
        }

        #region private code

        [DllImport("coredll.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int WaitForMultipleObjects(int count, IntPtr[] handle, bool waitAll, int milliseconds);

        #endregion
    }
}
