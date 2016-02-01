using System;
using System.IO;
using System.Threading;
using XLogger.Components.FileCycling;
using XLogger.Components.FileProvider;
using XLogger.Components.Message.C;
using XLogger.Components.OpenReasoning;
using XLogger.Components.Serializer;
using XLogger.Logger.ExternalAction;
using XLogger.Logger.File;
using XLogger.Logger.Gate;
using XLogger.Logger.ThreadSafe;
#if !WindowsCE
using XLogger.Components.FileWrapper.Zip;
#endif

namespace XLogger.Console
{
    public class TestWithZip
    {
        internal static void DoTest()
        {
            if (Directory.Exists(Consts.JournalLogPath))
            {
                Directory.Delete(Consts.JournalLogPath, true);
            }
            Directory.CreateDirectory(Consts.JournalLogPath);

            var logFileFormat = string.Format(
                "{0}.{{0}}.{{1}}.log",
                Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName)
                );

            var regexLogFileFormat = string.Format(
                "{0}.{{0}}.{{1}}.(log)|(zip)",
                Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName)
                );

            var serializer = new DefaultMessageSerializer();

            var cms = new CommonMessageSettings(
                serializer,
                Consts.SeparatorMessage,
                Consts.FirstMessage,
                Consts.LastMessage
                );

            var lfwf = new ZipLogFileWrapperFactory(
                cms
                );

            var fns = new DefaultFileProvider(
                Consts.JournalLogPath,
                logFileFormat,
                regexLogFileFormat,
                lfwf
                );

            var lfs = new LogFilesSettings(
                Consts.JournalLogPath,
                fns,
                3,
                600
                );

            var lmf = new CLogMessageFactory(
                serializer
                );

            var dor = new DefaultOpenReasoning(
                lfs
                );

            using (var rmw = new DefaultFileCycling(
                lfs,
                dor
                ))
            {
                using (var fdml = new FileMessageLogger(rmw, lmf))
                using (var eal = new ExternalActionLogger((message) => System.Console.Write(message), cms, lmf))
                {
                    using (var gl = new GateLogger(fdml, eal))
                    {
#if !WindowsCE
                        using (var tsl = new ThreadSafeLogger(gl))
#else
                        using (var tsl = new MonitorLogger(gl))
#endif
                        {
                            //try
                            //{
                            //    try
                            //    {
                            //        throw new InvalidOperationException("innner excp No " + 1);
                            //    }
                            //    catch (Exception excp)
                            //    {
                            //        throw new Exception("excp No " + 1, excp);
                            //    }
                            //}
                            //catch (Exception excp)
                            //{
                            //    tsl.LogException(
                            //        excp,
                            //        string.Empty
                            //        );
                            //}

                            for (var cc = 0; cc < 121; cc++)
                            {
                                //tsl.LogException(
                                //    new Exception("excp No " + cc, new InvalidOperationException("innner excp No " + cc)),
                                //    "mess No" + cc
                                //    );

                                //tsl.LogCategorizedMessage(
                                //    LogMessageCategoryEnum.Info,
                                //    "some source",
                                //    "01234567"
                                //    );

                                tsl.LogMessage(
                                    "01234567"
                                    );

                                Thread.Sleep(20);
                            }

                            Thread.Sleep(100);
                        }
                    }
                }
            }

            System.Console.WriteLine();
            System.Console.WriteLine("Press any key to continue....");
            System.Console.ReadLine();
        }
    }
}