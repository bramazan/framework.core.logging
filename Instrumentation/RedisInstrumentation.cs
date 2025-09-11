using Framework.Core.Logging.Logging.AppLogger;
using Microsoft.Extensions.Logging;

namespace Framework.Core.Logging.Instrumentation
{
    // Redis operations logging wrapper
    public static class RedisInstrumentation
    {
        private static IAppLogger? _appLogger;
        private static ILogger? _logger;
        private static RedisInstrumentationOptions? _options;

        public static void Initialize(IAppLogger appLogger, ILogger logger, RedisInstrumentationOptions options)
        {
            _appLogger = appLogger;
            _logger = logger;
            _options = options;
        }

        public static async Task<T> LogRedisOperationAsync<T>(
            string operation,
            string key,
            Func<Task<T>> redisOperation,
            object? value = null)
        {
            if (_appLogger == null || _options == null) 
                return await redisOperation();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString("N")[..8];

            try
            {
                // Log operation start
                if (_options.LogOperations)
                {
                    await LogRedisOperationStartAsync(operation, operationId, key, value);
                }

                var result = await redisOperation();
                stopwatch.Stop();

                // Log operation success
                if (_options.LogResults)
                {
                    await LogRedisOperationCompletedAsync(operation, operationId, key, stopwatch.ElapsedMilliseconds, true, result);
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Log operation error
                if (_options.LogErrors)
                {
                    await LogRedisOperationErrorAsync(operation, operationId, key, stopwatch.ElapsedMilliseconds, ex);
                }

                throw;
            }
        }

        public static async Task LogRedisOperationAsync(
            string operation,
            string key,
            Func<Task> redisOperation,
            object? value = null)
        {
            if (_appLogger == null || _options == null)
            {
                await redisOperation();
                return;
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString("N")[..8];

            try
            {
                // Log operation start
                if (_options.LogOperations)
                {
                    await LogRedisOperationStartAsync(operation, operationId, key, value);
                }

                await redisOperation();
                stopwatch.Stop();

                // Log operation success
                if (_options.LogResults)
                {
                    await LogRedisOperationCompletedAsync(operation, operationId, key, stopwatch.ElapsedMilliseconds, true);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Log operation error
                if (_options.LogErrors)
                {
                    await LogRedisOperationErrorAsync(operation, operationId, key, stopwatch.ElapsedMilliseconds, ex);
                }

                throw;
            }
        }

        private static async Task LogRedisOperationStartAsync(
            string operation, 
            string operationId, 
            string key, 
            object? value)
        {
            try
            {
                var logData = new
                {
                    Operation = operation,
                    OperationId = operationId,
                    Key = SanitizeKey(key),
                    Value = _options!.LogValues ? SanitizeValue(value) : null,
                    ValueSize = value?.ToString()?.Length ?? 0,
                    Timestamp = DateTimeOffset.UtcNow
                };

                _appLogger!.Log(
                    "{LogType} {Operation} {OperationId} {Key} Starting {LogData}",
                    LogType.Redis.ToString(),
                    operation,
                    operationId,
                    key,
                    System.Text.Json.JsonSerializer.Serialize(logData)
                );
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error logging Redis operation start");
            }

            await Task.CompletedTask;
        }

        private static async Task LogRedisOperationCompletedAsync(
            string operation, 
            string operationId, 
            string key,
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
                    Key = SanitizeKey(key),
                    ElapsedMilliseconds = elapsedMs,
                    Success = success,
                    HasResult = result != null,
                    ResultSize = result?.ToString()?.Length ?? 0,
                    Result = _options!.LogValues ? SanitizeValue(result) : null,
                    Timestamp = DateTimeOffset.UtcNow
                };

                // Log slow operations
                if (elapsedMs > _options.SlowOperationThresholdMs)
                {
                    _logger?.LogWarning("Slow Redis operation detected: {Operation} {Key} took {ElapsedMs}ms", 
                        operation, key, elapsedMs);
                }

                _appLogger!.Log(
                    "{LogType} {Operation} {OperationId} {Key} {ElapsedMs} {LogData}",
                    LogType.Redis.ToString(),
                    operation,
                    operationId,
                    key,
                    elapsedMs,
                    System.Text.Json.JsonSerializer.Serialize(logData)
                );
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error logging Redis operation completion");
            }

            await Task.CompletedTask;
        }

        private static async Task LogRedisOperationErrorAsync(
            string operation, 
            string operationId, 
            string key,
            long elapsedMs, 
            Exception exception)
        {
            try
            {
                var logData = new
                {
                    Operation = operation,
                    OperationId = operationId,
                    Key = SanitizeKey(key),
                    ElapsedMilliseconds = elapsedMs,
                    Success = false,
                    Error = exception.Message,
                    ExceptionType = exception.GetType().Name,
                    Timestamp = DateTimeOffset.UtcNow
                };

                _appLogger!.Log(
                    "{LogType} {Operation} {OperationId} {Key} {Error} {LogData}",
                    LogType.RedisError.ToString(),
                    operation,
                    operationId,
                    key,
                    exception.Message,
                    System.Text.Json.JsonSerializer.Serialize(logData)
                );
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error logging Redis operation error");
            }

            await Task.CompletedTask;
        }

        private static string SanitizeKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return key;

            // Mask sensitive keys
            foreach (var sensitivePattern in _options!.SensitiveKeyPatterns)
            {
                if (key.Contains(sensitivePattern, StringComparison.OrdinalIgnoreCase))
                {
                    return "***MASKED***";
                }
            }

            return key;
        }

        private static object? SanitizeValue(object? value)
        {
            if (value == null) return null;

            var stringValue = value.ToString();
            if (string.IsNullOrEmpty(stringValue)) return value;

            // Truncate large values
            if (stringValue.Length > _options!.MaxValueLength)
            {
                return stringValue.Substring(0, _options.MaxValueLength) + "...[TRUNCATED]";
            }

            // Mask sensitive values
            foreach (var sensitivePattern in _options.SensitiveValuePatterns)
            {
                if (stringValue.Contains(sensitivePattern, StringComparison.OrdinalIgnoreCase))
                {
                    return "***MASKED***";
                }
            }

            return value;
        }
    }

    public class RedisInstrumentationOptions
    {
        public bool LogOperations { get; set; } = true;
        public bool LogResults { get; set; } = false;
        public bool LogValues { get; set; } = false;
        public bool LogErrors { get; set; } = true;
        public double SlowOperationThresholdMs { get; set; } = 100;
        public int MaxValueLength { get; set; } = 1000;
        public string[] SensitiveKeyPatterns { get; set; } = { "password", "token", "secret", "key", "auth" };
        public string[] SensitiveValuePatterns { get; set; } = { "password", "token", "secret", "key" };
    }

    // Extension methods for common Redis operations
    public static class RedisInstrumentationExtensions
    {
        public static async Task<string?> GetAsync(this object redisClient, string key, Func<Task<string?>> operation)
        {
            return await RedisInstrumentation.LogRedisOperationAsync("GET", key, operation);
        }

        public static async Task SetAsync(this object redisClient, string key, object value, Func<Task> operation)
        {
            await RedisInstrumentation.LogRedisOperationAsync("SET", key, operation, value);
        }

        public static async Task<bool> DeleteAsync(this object redisClient, string key, Func<Task<bool>> operation)
        {
            return await RedisInstrumentation.LogRedisOperationAsync("DELETE", key, operation);
        }

        public static async Task<bool> ExistsAsync(this object redisClient, string key, Func<Task<bool>> operation)
        {
            return await RedisInstrumentation.LogRedisOperationAsync("EXISTS", key, operation);
        }
    }
}