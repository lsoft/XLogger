using System;
using System.Runtime.InteropServices;

namespace XLogger.Helper
{
    /// <summary>
    /// P/invoke implementation of syncronization event.
    /// </summary>
    internal class NoNamedEvent : IDisposable
    {
        /// <summary>
        /// Event handle.
        /// </summary>
        public IntPtr Handle
        {
            get;
            private set;
        }

        protected NoNamedEvent(
            bool isManualReset,
            bool initialState
            )
        {
            Handle = CreateEvent(IntPtr.Zero, isManualReset, initialState, null);

        }

        /// <summary>
        /// Set the event. Same as dispose.
        /// </summary>
        public void Set()
        {
            EventModify(this.Handle, (uint)EventFlagsEnum.SET);
        }

        /// <summary>
        /// Close (dispose) the event.
        /// </summary>
        public void Close()
        {
            this.Dispose();
        }

        /// <summary>
        /// Dispose the event. Same as close.
        /// </summary>
        public void Dispose()
        {
            if (this.Handle != IntPtr.Zero)
            {
                CloseHandle(this.Handle);

                this.Handle = IntPtr.Zero;
            }
        }

        #region private code

        [DllImport("coredll.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateEvent(IntPtr eventAttributes, bool isManualReset, bool initialState, string eventName);

        [DllImport("coredll.dll", SetLastError = true)]
        private static extern int EventModify(IntPtr handle, uint eventAction);

        [DllImport("coredll.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        private enum EventFlagsEnum
        {
            PULSE = 1,
            RESET = 2,
            SET = 3
        }

        #endregion
    }
}