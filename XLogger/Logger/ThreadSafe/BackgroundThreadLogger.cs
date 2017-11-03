using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using XLogger.Helper;
#if WindowsCE
using AutoResetEvent = XLogger.Helper.AutoResetEvent;
using ManualResetEvent = XLogger.Helper.ManualResetEvent;
#endif

namespace XLogger.Logger.ThreadSafe
{
    /// <summary>
    /// A decorator logger with thread safety.
    /// The class is too complex in the case when CPU has only 1 logical core or .Net is allowed to work only on 1 logical core.
    /// On devices with WindowsCE 5, WindowsCE 6 it's happens in 100% cases.
    /// On devices with WindowsCE 7 it's happens often.
    /// Consider using MonitorLogger instead.
    /// </summary>
    public class BackgroundThreadLogger : IMessageLogger2
    {
        /// <summary>
        /// Internal logger without thread safety.
        /// </summary>
        private readonly IMessageLogger _logger;

        /// <summary>
        /// 'New exception message has queued' event.
        /// </summary>
        private readonly AutoResetEvent _exceptionItemStoredEvent = new AutoResetEvent(false);

        /// <summary>
        /// 'New message has queued' event.
        /// </summary>
        private readonly AutoResetEvent _logItemStoredEvent = new AutoResetEvent(false);

        /// <summary>
        /// Logger stop event.
        /// </summary>
        private readonly ManualResetEvent _stopEvent = new ManualResetEvent(false);

        /// <summary>
        /// Exception message queue.
        /// </summary>
        private readonly ConcurrentQueue<ExceptionItemContainer> _exceptionQueue = new ConcurrentQueue<ExceptionItemContainer>();

        /// <summary>
        /// Message queue.
        /// </summary>
        private readonly ConcurrentQueue<LogItemContainer> _logQueue = new ConcurrentQueue<LogItemContainer>();

        /// <summary>
        /// Logger has started marker.
        /// </summary>
        private int _started = 0;

        /// <summary>
        /// Internal work thread.
        /// </summary>
        private Thread _workerThread;

        /// <summary>
        /// Dispose marker.
        /// </summary>
        private volatile bool _disposed = false;

        public BackgroundThreadLogger(
            IMessageLogger logger
            )
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            _logger = logger;
        }

        /// <summary>
        /// Write exception message to logger.
        /// </summary>
        public void LogException(Exception exception)
        {
            this.LogException(exception, string.Empty);
        }

        /// <summary>
        ///  Write exception message to logger.
        /// </summary>
        public void LogException(
            Exception exception,
            string message
            )
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (_disposed)
            {
                //skip a logging work when logger has disposed

                return;
            }

            //если еще не стартовали - стартуем
            if (Interlocked.CompareExchange(ref _started, 1, 0) == 0)
            {
                this.WorkStart();
            }

            var i = new ExceptionItemContainer(
                exception,
                message
                );

            _exceptionQueue.Enqueue(i);

            _exceptionItemStoredEvent.Set();
        }


        /// <summary>
        /// Write categorized message to logger.
        /// (it's easier to use LogMessage + LogWarning methods instead).
        /// </summary>
        public void LogCategorizedMessage(
            LogMessageCategoryEnum category,
            string source,
            string message
            )
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (_disposed)
            {
                //skip a logging work when logger has disposed

                return;
            }

            //если еще не стартовали - стартуем
            if (Interlocked.CompareExchange(ref _started, 1, 0) == 0)
            {
                this.WorkStart();
            }

            var i = new LogItemContainer(
                category,
                source,
                message
                );

            _logQueue.Enqueue(i);

            _logItemStoredEvent.Set();
        }

        /// <summary>
        /// Write message to logger.
        /// </summary>
        public void LogMessage(
            string message
            )
        {
            var source = StackHelper.GetClassNameFromStack();

            this.LogCategorizedMessage(
                LogMessageCategoryEnum.Info,
                source,
                message
                );
        }

        /// <summary>
        /// Write message to logger.
        /// </summary>
        public void LogWarning(
            string message
            )
        {
            var source = StackHelper.GetClassNameFromStack();

            this.LogCategorizedMessage(
                LogMessageCategoryEnum.Warning,
                source,
                message
                );
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                _stopEvent.Set();

                if (_workerThread != null)
                {
                    _workerThread.Join();
                }

                _exceptionItemStoredEvent.Close();
                _logItemStoredEvent.Close();
                _stopEvent.Close();
            }
        }


        #region private code

        private void WorkStart()
        {
            var t = new Thread(WorkThread);
            _workerThread = t;

            _workerThread.Start();
        }

        private void WorkThread()
        {
            while (true)
            {
                try
                {
                    //wait for any event
#if WindowsCE
                    var waitIndex = WaitHandleHelper.WaitAny(
                        new NoNamedEvent[]
#else
                    var waitIndex = WaitHandle.WaitAny(
                        new WaitHandle[]
#endif
                        {
                            _stopEvent,
                            _exceptionItemStoredEvent,
                            _logItemStoredEvent
                        },
                        -1);

                    //process action
                    switch (waitIndex)
                    {
                        case 0:
#if WindowsCE
                        case WaitHandleHelper.WaitTimeout:
#else
                        case WaitHandle.WaitTimeout:
#endif
                            ProcessExceptionQueue();
                            ProcessLogQueue();
                            return;
                        case 1:
                            ProcessExceptionQueue();
                            break;
                        case 2:
                            ProcessLogQueue();
                            break;
                    }
                }
                catch
                {
                    //nothing can be done here
                    //force this error to be skipped

                    //perform some timeout for a CPU time economy in case of permanent exception
                    Thread.Sleep(100);
                }
            }
        }

        private void ProcessLogQueue()
        {
            LogItemContainer item;
            while (_logQueue.TryDequeue(out item))
            {
                try
                {
                    _logger.LogCategorizedMessage(
                        item.Category,
                        item.Source,
                        item.Message
                        );
                }
                catch
                {
                    //no way to log any logger error
                    //nothing can be done here

                    //it's better to lose a dequeued item due to
                    //out of memory risk in case of this error is permanent
                }
            }
        }

        private void ProcessExceptionQueue()
        {
            ExceptionItemContainer item;
            while(_exceptionQueue.TryDequeue(out item))
            {
                try
                {
                    _logger.LogException(item.Exception, item.Message);
                }
                catch
                {
                    //no way to log any logger error
                    //nothing can be done here

                    //it's better to lose a dequeued item due to
                    //out of memory risk in case of this error is permanent
                }
            }
        }

        /// <summary>
        /// Internal container class
        /// </summary>
        private class LogItemContainer
        {
            public LogMessageCategoryEnum Category
            {
                get;
                private set;
            }

            public string Source
            {
                get;
                private set;
            }

            public string Message
            {
                get;
                private set;
            }

            public LogItemContainer(
                LogMessageCategoryEnum category,
                string source,
                string message
                )
            {
                if (source == null)
                {
                    throw new ArgumentNullException("source");
                }
                if (message == null)
                {
                    throw new ArgumentNullException("message");
                }

                Category = category;
                Source = source;
                Message = message;
            }
        }

        /// <summary>
        /// Internal container class
        /// </summary>
        private class ExceptionItemContainer
        {
            public Exception Exception
            {
                get;
                private set;
            }

            public string Message
            {
                get;
                private set;
            }

            public ExceptionItemContainer(Exception exception, string message)
            {
                if (exception == null)
                {
                    throw new ArgumentNullException("exception");
                }
                if (message == null)
                {
                    throw new ArgumentNullException("message");
                }
                Exception = exception;
                Message = message;
            }
        }


        #endregion

    }
}
