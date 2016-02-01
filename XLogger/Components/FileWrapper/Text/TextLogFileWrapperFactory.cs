using System;
using XLogger.Logger;

namespace XLogger.Components.FileWrapper.Text
{
    /// <summary>
    /// Text log file wrappers factory.
    /// </summary>
    public class TextLogFileWrapperFactory : ILogFileWrapperFactory
    {
        private readonly ICommonMessageSettings _commonMessageSettings;

        public TextLogFileWrapperFactory(
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
                new TextLogFileWrapper(
                    _commonMessageSettings,
                    createDate,
                    filePath,
                    fileOpenMode
                    );
        }
    }
}