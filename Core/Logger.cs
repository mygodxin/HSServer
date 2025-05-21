using NLog;
using NLog.Config;

namespace Core
{
    /// <summary>
    /// 基于进程ID的日志记录器
    /// </summary>
    public class Logger : Singleton<Logger>
    {
        // NLog日志记录器
        private static NLog.Logger _logger;

        // 当前进程ID
        private static int _processId;

        public static void Init()
        {
            LogManager.Setup();
            LogManager.Configuration = new XmlLoggingConfiguration("Config/NLog.config");
            LogManager.AutoShutdown = false;

            _processId = System.Diagnostics.Process.GetCurrentProcess().Id;
            _logger = LogManager.GetCurrentClassLogger();

            // 配置NLog布局渲染器以包含进程ID
            var configuration = LogManager.Configuration;
            if (configuration != null)
            {
                var logEventInfo = new LogEventInfo
                {
                    TimeStamp = DateTime.Now,
                    Level = LogLevel.Info,
                    LoggerName = _logger.Name,
                    Message = "[Logger initialized for process]" + _processId
                };

                _logger.Log(logEventInfo);
            }
        }

        /// <summary>
        /// 记录调试信息
        /// </summary>
        public static void Debug(string message, params object[] args)
        {
            var logEvent = CreateLogEvent(LogLevel.Debug, message, args);
            _logger.Log(logEvent);
        }

        /// <summary>
        /// 记录一般信息
        /// </summary>
        public static void Info(string message, params object[] args)
        {
            var logEvent = CreateLogEvent(LogLevel.Info, message, args);
            _logger.Log(logEvent);
        }

        /// <summary>
        /// 记录警告信息
        /// </summary>
        public static void Warn(string message, params object[] args)
        {
            var logEvent = CreateLogEvent(LogLevel.Warn, message, args);
            _logger.Log(logEvent);
        }

        /// <summary>
        /// 记录错误信息
        /// </summary>
        public static void Error(string message, params object[] args)
        {
            var logEvent = CreateLogEvent(LogLevel.Error, message, args);
            _logger.Log(logEvent);
        }

        /// <summary>
        /// 记录异常信息
        /// </summary>
        public static void Error(Exception ex, string message, params object[] args)
        {
            var logEvent = CreateLogEvent(LogLevel.Error, message, args);
            logEvent.Exception = ex;
            _logger.Log(logEvent);
        }

        /// <summary>
        /// 创建带有进程ID的日志事件
        /// </summary>
        private static LogEventInfo CreateLogEvent(LogLevel level, string message, params object[] args)
        {
            var logEvent = new LogEventInfo
            {
                Level = level,
                LoggerName = _logger.Name,
                Message = $"{string.Format(message, args)}",
                TimeStamp = DateTime.Now
            };
            return logEvent;
        }
    }
}
