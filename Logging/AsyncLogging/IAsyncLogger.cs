using Microsoft.Extensions.Logging;

namespace Framework.Core.Logging.Logging.AsyncLogging
{
    public interface IAsyncLogger
    {
        ValueTask LogAsync(string messageTemplate, params object?[]? propertyValues);
        ValueTask LogAsync(LogLevel level, string messageTemplate, params object?[]? propertyValues);
        ValueTask LogAsync(LogLevel level, Exception? exception, string messageTemplate, params object?[]? propertyValues);
        Task FlushAsync(CancellationToken cancellationToken = default);
    }

    public class LogEntry
    {
        public LogLevel Level { get; set; }
        public string MessageTemplate { get; set; } = string.Empty;
        public object?[]? PropertyValues { get; set; }
        public Exception? Exception { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = string.Empty;

        public void Reset()
        {
            Level = LogLevel.Information;
            MessageTemplate = string.Empty;
            PropertyValues = null;
            Exception = null;
            Timestamp = default;
            CorrelationId = string.Empty;
            MachineName = string.Empty;
            ApplicationName = string.Empty;
        }
    }
}