using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace TimeShiftLoggerExample
{
    public class BufferedLoggingProvider(string bufferedLogFilePath) : ILoggerProvider, IDisposable
    {
        private readonly ConcurrentDictionary<string, BufferedLogger> _loggers = new();

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new BufferedLogger(name, bufferedLogFilePath));
        }

        public void Dispose()
        {
            _loggers.Clear();
            GC.SuppressFinalize(this); // Suppress finalization to adhere to CA1816
        }
    }

    public class BufferedLogger(string categoryName, string bufferedLogFilePath) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return new LoggingScope(bufferedLogFilePath, state.ToString());
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true; // Enable all log levels
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            var scope = LoggingScope.CurrentScope.Value;

            if (scope != null)
            {
                scope.AddLog($"[{logLevel}] {categoryName}: {message}");
                if (exception != null)
                {
                    scope.MarkExceptionOccurred();
                }
            }
        }

        private class LoggingScope : IDisposable
        {
            private readonly string _bufferedLogFilePath;
            private readonly List<string> _logBuffer = [];
            private bool _hasException = false;
            public static AsyncLocal<LoggingScope?> CurrentScope { get; } = new();

            public LoggingScope(string bufferedLogFilePath, string? scopeName)
            {
                _bufferedLogFilePath = Path.ChangeExtension(bufferedLogFilePath, $"{scopeName}{Path.GetExtension(bufferedLogFilePath)}");
                CurrentScope.Value = this;
            }

            public void AddLog(string log)
            {
                _logBuffer.Add(log);
            }

            public void MarkExceptionOccurred()
            {
                _hasException = true;
            }

            public void Dispose()
            {
                if (_hasException)
                {
                    File.AppendAllLines(_bufferedLogFilePath, _logBuffer);
                }
                CurrentScope.Value = null;
            }
        }
    }
}
