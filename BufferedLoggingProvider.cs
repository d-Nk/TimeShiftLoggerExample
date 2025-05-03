using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;


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
            var scope = LoggingScope.CurrentScope.Value;
            if(scope != null)
            {
                return scope.PushScope(state);
            }
            else
            {
                // トップレベルのスコープを追加
                return new LoggingScope(bufferedLogFilePath, state.ToString());
            }
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
                // スコープにプッシュしておく
                scope.AddLog($"[{logLevel}] {categoryName}: {message}");
                if (exception != null)
                {
                    // 例外が発生した場合は、スコープにマークしておく
                    scope.MarkExceptionOccurred();
                }
            }
        }

        private class LoggingScope : IDisposable
        {
            private readonly string _bufferedLogFilePath;
            private readonly List<string> _logBuffer = [];
            private bool _hasException = false;
            private readonly List<object> scopes = [];
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
                    // 例外が発生した場合は、スコープをファイルに書き込む  
                    File.AppendAllLines(_bufferedLogFilePath, _logBuffer);
                }
                CurrentScope.Value = null;
            }

            internal IDisposable PushScope(object scope)
            {
                scopes.Add(scope);
                return new ScopeDisposer(scopes, scope);
            }

            private class ScopeDisposer(List<object> scopes, object scope) : IDisposable
            {
                private readonly List<object> _scopes = scopes;

                public void Dispose()
                {
                    _scopes.Remove(scope);
                }
            }
        }
    }
}
