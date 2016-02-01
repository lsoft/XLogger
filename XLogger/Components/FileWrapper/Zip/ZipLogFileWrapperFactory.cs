using System;
using XLogger.Logger;

namespace XLogger.Components.FileWrapper.Zip
{
    /// <summary>
    /// Zip log file wrappers factory.
    /// </summary>
    public class ZipLogFileWrapperFactory : ILogFileWrapperFactory
    {
        private readonly ICommonMessageSettings _commonMessageSettings;

        public ZipLogFileWrapperFactory(
            ICommonMessageSettings commonMessageSettings
            )
        {
            if (commonMessageSettings == null)
            {
                throw new ArgumentNullException("commonMessageSettings");
            }

            _commonMessageSettings = commonMessageSettings;
        }

        public ILogFileWrapper CreateLogFileWrapper(
            DateTime createDate,
            string filePath,
            LogFileOpenModeEnum fileOpenMode
            )
        {
            return
                new ZipLogFileWrapper(
                    _commonMessageSettings,
                    createDate,
                    filePath,
                    fileOpenMode
                    );
        }
    }
}