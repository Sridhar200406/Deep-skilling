using System.Collections.Concurrent;

namespace Week3_EmployeeManagementAPI.Services
{
    /// <summary>
    /// A lightweight file-based ILoggerProvider that writes log entries
    /// to daily-rolling files in the configured directory.
    /// Pattern: Logs/app-yyyy-MM-dd.log
    /// </summary>
    public sealed class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _logDirectory;
        private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();

        public FileLoggerProvider(string logDirectory)
        {
            _logDirectory = logDirectory;
            Directory.CreateDirectory(_logDirectory);
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _logDirectory));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }

    /// <summary>
    /// Individual logger that writes formatted log lines to a daily file.
    /// Thread-safe through file locking.
    /// </summary>
    public sealed class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly string _logDirectory;
        private static readonly object _lock = new();

        public FileLogger(string categoryName, string logDirectory)
        {
            _categoryName = categoryName;
            _logDirectory = logDirectory;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var logFile = Path.Combine(_logDirectory, $"app-{DateTime.Now:yyyy-MM-dd}.log");
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{logLevel,-12}] [{_categoryName}] {formatter(state, exception)}";

            if (exception != null)
            {
                logEntry += Environment.NewLine + exception;
            }

            lock (_lock)
            {
                File.AppendAllText(logFile, logEntry + Environment.NewLine);
            }
        }
    }
}
