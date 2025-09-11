using Framework.Core.Logging.Logging.AppLogger;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace Framework.Core.Logging.Instrumentation
{
    // Generic database logging wrapper - works with any ADO.NET provider
    public static class DatabaseInstrumentation
    {
        private static IAppLogger? _appLogger;
        private static ILogger? _logger;
        private static DatabaseInstrumentationOptions? _options;

        public static void Initialize(IAppLogger appLogger, ILogger logger, DatabaseInstrumentationOptions options)
        {
            _appLogger = appLogger;
            _logger = logger;
            _options = options;
        }

        public static async Task<T> LogDbOperationAsync<T>(
            string operation,
            Func<Task<T>> dbOperation,
            string? commandText = null,
            Dictionary<string, object?>? parameters = null)
        {
            if (_appLogger == null || _options == null)
                return await dbOperation();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString("N")[..8];

            try
            {
                // Log operation start
                if (_options.LogOperations)
                {
                    await LogDbOperationStartAsync(operation, operationId, commandText, parameters);
                }

                var result = await dbOperation();
                stopwatch.Stop();

                // Log operation success
                if (_options.LogResults)
                {
                    await LogDbOperationCompletedAsync(operation, operationId, stopwatch.ElapsedMilliseconds, true, result);
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Log operation error
                if (_options.LogErrors)
                {
                    await LogDbOperationErrorAsync(operation, operationId, stopwatch.ElapsedMilliseconds, ex, commandText);
                }

                throw;
            }
        }

        private static async Task LogDbOperationStartAsync(
            string operation,
            string operationId,
            string? commandText,
            Dictionary<string, object?>? parameters)
        {
            try
            {
                var sanitizedCommandText = _options!.LogSqlParameters
                    ? commandText
                    : SanitizeSqlCommand(commandText);

                var logData = new
                {
                    Operation = operation,
                    OperationId = operationId,
                    CommandText = sanitizedCommandText,
                    Parameters = _options.LogSqlParameters ? SanitizeParameters(parameters) : null,
                    Timestamp = DateTimeOffset.UtcNow
                };

                _appLogger!.Log(
                    "{LogType} {Operation} {OperationId} Starting {LogData}",
                    LogType.Database.ToString(),
                    operation,
                    operationId,
                    System.Text.Json.JsonSerializer.Serialize(logData)
                );
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error logging database operation start");
            }

            await Task.CompletedTask;
        }

        private static async Task LogDbOperationCompletedAsync(
            string operation,
            string operationId,
            long elapsedMs,
            bool success,
            object? result = null)
        {
            try
            {
                var logData = new
                {
                    Operation = operation,
                    OperationId = operationId,
                    ElapsedMilliseconds = elapsedMs,
                    Success = success,
                    HasResult = result != null,
                    RecordsAffected = result is int recordCount ? recordCount : (int?)null,
                    Timestamp = DateTimeOffset.UtcNow
                };

                // Log slow queries
                if (elapsedMs > _options!.SlowQueryThresholdMs)
                {
                    _logger?.LogWarning("Slow database operation detected: {Operation} {OperationId} took {ElapsedMs}ms",
                        operation, operationId, elapsedMs);
                }

                _appLogger!.Log(
                    "{LogType} {Operation} {OperationId} {ElapsedMs} {LogData}",
                    LogType.Database.ToString(),
                    operation,
                    operationId,
                    elapsedMs,
                    System.Text.Json.JsonSerializer.Serialize(logData)
                );
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error logging database operation completion");
            }

            await Task.CompletedTask;
        }

        private static async Task LogDbOperationErrorAsync(
            string operation,
            string operationId,
            long elapsedMs,
            Exception exception,
            string? commandText = null)
        {
            try
            {
                var logData = new
                {
                    Operation = operation,
                    OperationId = operationId,
                    ElapsedMilliseconds = elapsedMs,
                    Success = false,
                    Error = exception.Message,
                    ExceptionType = exception.GetType().Name,
                    CommandText = _options!.LogSqlOnError ? commandText : null,
                    Timestamp = DateTimeOffset.UtcNow
                };

                _appLogger!.Log(
                    "{LogType} {Operation} {OperationId} {Error} {LogData}",
                    LogType.DatabaseError.ToString(),
                    operation,
                    operationId,
                    exception.Message,
                    System.Text.Json.JsonSerializer.Serialize(logData)
                );
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error logging database operation error");
            }

            await Task.CompletedTask;
        }

        private static string? SanitizeSqlCommand(string? sql)
        {
            if (string.IsNullOrEmpty(sql)) return sql;
            
            // Simple parameter sanitization
            return System.Text.RegularExpressions.Regex.Replace(sql, @"@\w+", "@***");
        }

        private static Dictionary<string, object?>? SanitizeParameters(Dictionary<string, object?>? parameters)
        {
            if (parameters == null) return null;

            var sanitized = new Dictionary<string, object?>();
            
            foreach (var param in parameters)
            {
                var value = param.Value;
                
                // Mask sensitive parameter names
                if (_options!.SensitiveParameterNames.Any(name =>
                    param.Key.Contains(name, StringComparison.OrdinalIgnoreCase)))
                {
                    value = "***MASKED***";
                }
                
                sanitized[param.Key] = value;
            }
            
            return sanitized;
        }
    }

    public class DatabaseInstrumentationOptions
    {
        public bool LogOperations { get; set; } = true;
        public bool LogResults { get; set; } = true;
        public bool LogErrors { get; set; } = true;
        public bool LogSqlParameters { get; set; } = false;
        public bool LogSqlOnError { get; set; } = true;
        public double SlowQueryThresholdMs { get; set; } = 1000;
        public string[] SensitiveParameterNames { get; set; } = { "password", "token", "secret", "key", "pwd" };
    }
}