using System;

namespace XLogger.Helper
{
    /// <summary>
    /// Logger ambient context.
    /// </summary>
    internal static class AmbientContext
    {
        private static volatile Func<DateTime> _nowProvider;

        static AmbientContext()
        {
            _nowProvider = () => DateTime.Now;
        }

        public static void ForceToRewrite(
            Func<DateTime> nowProvider
            )
        {
            if (nowProvider == null)
            {
                throw new ArgumentNullException("nowProvider");
            }

            System.Threading.Interlocked.Exchange(
                ref _nowProvider,
                nowProvider
                );
        }

        public static DateTime Now
        {
            get
            {
                //get local copy
                var nowProvider = _nowProvider;

                //take a result
                var result = nowProvider();

                //and return it
                return result;
            }
        }

    }
}
