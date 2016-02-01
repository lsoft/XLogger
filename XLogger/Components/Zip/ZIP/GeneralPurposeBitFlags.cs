using System;

namespace XLogger.Components.Zip.ZIP
{
    [Flags]
    public enum GeneralPurposeBitFlags : short
    {
        SizeAfterData = 0x0008,
        Unicode = 0x0800,
    }
}