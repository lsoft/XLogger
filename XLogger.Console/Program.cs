using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using XLogger.Components;
using XLogger.Components.FileCycling;
using XLogger.Components.FileProvider;
using XLogger.Components.FileWrapper.Text;
using XLogger.Components.Message;
using XLogger.Components.Message.C;
using XLogger.Components.OpenReasoning;
using XLogger.Components.Serializer;
using XLogger.Helper;
using XLogger.Logger.ExternalAction;
using XLogger.Logger.File;
using XLogger.Logger.Gate;
using XLogger.Logger.ThreadSafe;

namespace XLogger.Console
{
    class Program
    {

        static void Main(string[] args)
        {
#if !WindowsCE
            TestWithZip.DoTest();
#else
            TestWithText.DoTest();
#endif
        }
    }
}
