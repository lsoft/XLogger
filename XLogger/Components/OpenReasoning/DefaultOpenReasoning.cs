using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XLogger.Components.FileWrapper;
using XLogger.Helper;
using XLogger.Logger.File;

namespace XLogger.Components.OpenReasoning
{
    /// <summary>
    /// Default implementation of the worker.
    /// </summary>
    public class DefaultOpenReasoning : IOpenReasoning
    {
        private readonly ILogFilesSettings _logFilesSettings;

        public DefaultOpenReasoning(
            ILogFilesSettings logFilesSettings
            )
        {
            if (logFilesSettings == null)
            {
                throw new ArgumentNullException("logFilesSettings");
            }

            _logFilesSettings = logFilesSettings;
        }

        public bool IsNeedToOpenNewFile(
            ILogFileWrapper current
            )
        {
            //current allowed to be null

            var needToOpenNewFile = false;

            if (current == null || !current.IsActive)
            {
                needToOpenNewFile = true;
            }

            if (current != null && current.IsActive)
            {
                if (current.FileSize >= _logFilesSettings.MaxFileSize)
                {
                    //файл на замену - по размеру
                    needToOpenNewFile = true;
                }
            }

            if (current != null && current.IsActive)
            {
                var now = AmbientContext.Now;

                if (now.Date != current.CreateDate.Date)
                {
                    //файл на замену - дата сменилась
                    needToOpenNewFile = true;
                }
            }

            return
                needToOpenNewFile;
        }
    }
}
