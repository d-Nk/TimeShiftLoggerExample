using Microsoft.Extensions.Logging;
using System;

namespace TimeShiftLoggerExample
{
    internal partial class PeriodicProcessor(ILoggerFactory loggerFactory)
    {
        private readonly ILogger<PeriodicProcessor> _logger = loggerFactory.CreateLogger<PeriodicProcessor>();

#pragma warning disable IDE0060 // 未使用のパラメーターを削除します
        public void Execute(object? state)
#pragma warning restore IDE0060 // 未使用のパラメーターを削除します
        {
            var currentTime = DateTime.Now;

            using (_logger.BeginScope(currentTime.ToString("yyyy_MM_dd_HH_mm_ss_FFF")))
            {
                try
                {
                    int a = GenerateRandomNumber(10);
                    int b = GenerateRandomNumber(3);
                    double result = DivideNumbers(a, b);
                    LogExecutionResult(_logger, a, b, result);
                }
                catch (Exception ex)
                {
                    // Log any exception as an error
                    LogExecutionError(_logger, ex);
                }
            }
        }

        private int GenerateRandomNumber(int max)
        {
            var random = new Random();
            int value = random.Next(0, max + 1); // Generate a random number between 0 and max
            
            // Use the generated debug logging method
            LogGeneratedRandomNumber(_logger, value, max);
            
            return value;
        }

        private static double DivideNumbers(int a, int b)
        {
            if (b == 0)
            {
                throw new ApplicationException($"b must not be 0");
            }
            return (double)a / b; // Perform division
        }

        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Generated random number: {Value} with max: {Max}")]
        private static partial void LogGeneratedRandomNumber(ILogger logger, int value, int max);

        [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "A: {A}, B: {B}, Result: {Result}")]
        private static partial void LogExecutionResult(ILogger logger, int a, int b, double result);

        [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "An error occurred during execution.")]
        private static partial void LogExecutionError(ILogger logger, Exception exception);
    }
}
