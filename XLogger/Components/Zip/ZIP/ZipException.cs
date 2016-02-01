using System;

namespace XLogger.Components.Zip.ZIP
{
    public class ZipException : ApplicationException
    {
        public ZipException(string message)
            : base("Zip exception." + message)
        {
        }
    }
}