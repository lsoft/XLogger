using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using XLogger.Components.FileWrapper;
using XLogger.Helper;

namespace XLogger.Components.FileProvider
{
    /// <summary>
    /// Default implementations of log file provider.
    /// </summary>
    public class DefaultFileProvider : IFileProvider
    {
        /// <summary>
        /// Log folder.
        /// </summary>
        private readonly string _logFolder;

        /// <summary>
        /// Factory of log file wrappers.
        /// </summary>
        private readonly ILogFileWrapperFactory _logFileWrapperFactory;

        /// <summary>
        /// Last log file create date.
        /// </summary>
        private DateTime _date;
        
        /// <summary>
        /// Current log file index.
        /// </summary>
        private int _lastLogFileIndex = 0;

        /// <summary>
        /// Log file name format.
        /// </summary>
        private readonly string _fileNameFormat;

        /// <summary>
        /// Regexp for mathing existing files against log file name scheme.
        /// </summary>
        private readonly Regex _fileNameMatcher;

        /// <summary>
        /// Existing log files.
        /// </summary>
        private readonly List<ILogFileWrapper> _logFiles = new List<ILogFileWrapper>();

        public DefaultFileProvider(
            string logFolder,
            string fileNameFormat,
            string regexLogFileFormat,
            ILogFileWrapperFactory logFileWrapperFactory
            )
        {
            if (logFolder == null)
            {
                throw new ArgumentNullException("logFolder");
            }
            if (fileNameFormat == null)
            {
                throw new ArgumentNullException("fileNameFormat");
            }
            if (regexLogFileFormat == null)
            {
                throw new ArgumentNullException("regexLogFileFormat");
            }
            if (logFileWrapperFactory == null)
            {
                throw new ArgumentNullException("logFileWrapperFactory");
            }

            _logFolder = logFolder;
            _logFileWrapperFactory = logFileWrapperFactory;

            _fileNameFormat = fileNameFormat;
            _fileNameMatcher = new Regex(
                string.Format(regexLogFileFormat.Replace(".", @"\."), @"(?<date>\d{4}-\d{2}-\d{2})", @"(?<idx>\d+)"),
                RegexOptions.Compiled);

            _logFiles  = EnumerateLogFilesInOrderOfCreation().ToList();

            var lastLogFile = _logFiles.LastOrDefault();
            if (lastLogFile != null)
            {
                _lastLogFileIndex = ParseIndex(lastLogFile.FileName);
                _date = ParseDate(lastLogFile.FileName).Date;
            }
            else
            {
                _lastLogFileIndex = -1;
                _date = AmbientContext.Now;
            }
        }


        public ILogFileWrapper CreateNextFile()
        {
            var nextFileName = GenerateNextFileName(
                );

            var nextFileWrapper = CreateFileWrapperByFileName(
                AmbientContext.Now.Date,
                nextFileName,
                LogFileOpenModeEnum.CreateNew
                );

            Debug.WriteLine("create: " + nextFileName);

            _logFiles.Add(nextFileWrapper);

            return
                nextFileWrapper;
        }

        public ILogFileWrapper GetLastFile()
        {
            return
                _logFiles.LastOrDefault();
        }

        public void DeleteOldFiles(int maxFileCount)
        {
            for (var cc = 0; cc < _logFiles.Count - maxFileCount; cc++)
            {
                Debug.WriteLine("delete: " + _logFiles[0].FileName);

                _logFiles[0].Delete();

                _logFiles.RemoveAt(0);
            }
        }

        #region private code

        private ILogFileWrapper CreateFileWrapperByFileName(
            DateTime createDate,
            string fileName,
            LogFileOpenModeEnum logFileOpenMode
            )
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            var nextFilePath = Path.Combine(
                _logFolder,
                fileName
                );

            var result = _logFileWrapperFactory.CreateLogFileWrapper(
                createDate,
                nextFilePath,
                logFileOpenMode
                );

            return
                result;
        }

        private IEnumerable<ILogFileWrapper> EnumerateLogFilesInOrderOfCreation()
        {
            var fileNames =
                (from filename in Directory.GetFiles(_logFolder) //get all files from log folder
                where _fileNameMatcher.IsMatch(filename) //filter it against regexp to take only log file and skip other files
                let date = ParseDate(filename).Date //get create date of the log file
                let index = ParseIndex(filename) //get log file index of the log file
                let key = date.Ticks + index //compose ordering key
                orderby key ascending //do ordering (take a look of method's name :) )
                let fn = new FileInfo(filename) //get file info to ...
                select fn.Name //... select only filename without a folder name
                ).ToList(); //and return it in the list, we must know count of file names

            //creating wrappers...
            var result = new List<ILogFileWrapper>();
            for (var cc = 0; cc < fileNames.Count; cc++)
            {
                var fn = fileNames[cc];

                var date = ParseDate(fn).Date;

                var fp = Path.Combine(
                    _logFolder,
                    fn
                    );

                var fw = _logFileWrapperFactory.CreateLogFileWrapper(
                    date,
                    fp, 
                    (cc < (fileNames.Count - 1))
                        ? LogFileOpenModeEnum.NoCreate
                        : LogFileOpenModeEnum.AppendToExisting
                    );
                
                result.Add(fw);
            }

            return
                result;
        }

        private string GenerateNextFileName(
            )
        {
            var now = AmbientContext.Now;

            if (now.Date == _date.Date)
            {
                _lastLogFileIndex++;
            }
            else
            {
                _lastLogFileIndex = 0;
                _date = now;
            }

            var mask = GetMask();

            var result = string.Format(
                _fileNameFormat,
                _date.ToString("yyyy-MM-dd"),
                _lastLogFileIndex.ToString(mask)
                );

            return
                result;
        }

        private DateTime ParseDate(
            string fileName
            )
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            var group = _fileNameMatcher.Match(fileName).Groups["date"].Value;

            return
                DateTime.ParseExact(
                    group,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture
                    );
        }

        private int ParseIndex(
            string fileName
            )
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            var group = _fileNameMatcher.Match(fileName).Groups["idx"].Value;

            return
                int.Parse(group);
        }


        private string GetMask()
        {
            if (_lastLogFileIndex < 1000)
            {
                return
                    "000";
            }

            var maskLength = (int)(Math.Log10(_lastLogFileIndex) + 1);

            return
                new string('0', maskLength);
        }

        #endregion
    }
}