using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Json;

namespace TimeShiftLoggerExample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(new JsonFormatter() ,"Logs/app.json", rollingInterval: RollingInterval.Day, buffered:false)
                .CreateLogger();

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(); // Use Serilog for logging
            });
            var logger = loggerFactory.CreateLogger<Program>();

            // Initialize and start the timer
            var processor = new PeriodicProcessor(loggerFactory);
            using var timer = new Timer(processor.Execute, null, 0, 2000); // 2 second interval

            logger.LogInformation("Timer started. Press Enter to exit...");
            Console.ReadLine();

            // Timer will be disposed automatically due to 'using'
            logger.LogInformation("Timer stopped.");
            Log.CloseAndFlush(); // Ensure Serilog flushes logs
        }
    }
}