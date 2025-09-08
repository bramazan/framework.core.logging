using Framework.Core.Logging.Logging.AppLogger;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Framework.Core.Logging.Instrumentation
{
    // Background service logging wrapper
    public static class BackgroundServiceInstrumentation
    {
        private static IAppLogger? _appLogger;
        private static ILogger? _logger;
        private static BackgroundServiceInstrumentationOptions? _options;

        public static void Initialize(IAppLogger appLogger, ILogger logger, BackgroundServiceInstrumentationOptions options)
        {
            _appLogger = appLogger;
            _logger = logger;
            _options = options;
        }

        public static async Task LogBackgroundOperationAsync(
            string serviceName,
            string operationName,
            Func<Task> operation,
            CancellationToken cancellationToken = default)
        {
            if (_appLogger == null || _options == null)
            {
                await operation();
                return;
            }

            var operationId = Guid.NewGuid().ToString("N")[..8];
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Log operation start
                if (_options.LogOperations)
                {
                    await LogBackgroundOperationStartAsync(serviceName, operationName, operationId);
                }

                await operation();
                stopwatch.Stop();

                // Log operation success
                if (_options.LogResults)
                {
                    await LogBackgroundOperationCompletedAsync(serviceName, operationName, operationId, stopwatch.ElapsedMilliseconds, true);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                
                if (_options.LogCancellations)
                {
                    await LogBackgroundOperationCancelledAsync(serviceName, operationName, operationId, stopwatch.ElapsedMilliseconds);
                }
                
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Log operation error
                if (_options.LogErrors)
                {
                    await LogBackgroundOperationErrorAsync(serviceName, operationName, operationId, stopwatch.ElapsedMilliseconds, ex);
                }

                throw;
            }
        }

        public static async Task<T> LogBackgroundOperationAsync<T>(
            string serviceName,
            string operationName,
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            if (_appLogger == null || _options == null)
                return await operation();

            var operationId = Guid.NewGuid().ToString("N")[..8];
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Log operation start
                if (_options.LogOperations)
                {
                    await LogBackgroundOperationStartAsync(serviceName, operationName, operationId);
                }

                var result = await operation();
                stopwatch.Stop();

                // Log operation success
                if (_options.LogResults)
                {
                    await LogBackgroundOperationCompletedAsync(serviceName, operationName, operationId, stopwatch.ElapsedMilliseconds, true, result);
                }

                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                
                if (_options.LogCancellations)
                {
                    await LogBackgroundOperationCancelledAsync(serviceName, operationName, operationId, stopwatch.ElapsedMilliseconds);
                }
                
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Log operation error
                if (_options.LogErrors)
                {
                    await LogBackgroundOperationErrorAsync(serviceName, operationName, operationId, stopwatch.ElapsedMilliseconds, ex);
                }

                throw;
            }
        }

        private static async Task LogBackgroundOperationStartAsync(
            string serviceName,
            string operationName,
            string operationId)
        {
            try
            {
                var logData = new
                {
                    ServiceName = serviceName,
                    OperationName = operationName,
                    OperationId = operationId,
                    ThreadId = Environment.CurrentManagedThreadId,
                    ProcessId = Environment.ProcessId,
                    Timestamp = DateTimeOffset.UtcNow
                };

                _appLogger!.Log(
                    "{LogType} {ServiceName} {OperationName} {OperationId} Starting {LogData}",
                    LogType.BackgroundService.ToString(),
                    serviceName,
                    operationName,
                    operationId,
                    Newtonsoft.Json.JsonConvert.SerializeObject(logData, Newtonsoft.Json.Formatting.None)
                );
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error logging background service operation start");
            }

            await Task.CompletedTask;
        }

        private static async Task LogBackgroundOperationCompletedAsync(
            string serviceName,
            string operationName,
            string operationId,
            long elapsedMs,
            bool success,
            object? result = null)
        {
            try
            {
                var logData = new
                {
                    ServiceName = serviceName,
                    OperationName = operationName,
                    OperationId = operationId,
                    ElapsedMilliseconds = elapsedMs,
                    Success = success,
                    HasResult = result != null,
                    ResultType = result?.GetType().Name,
                    ThreadId = Environment.CurrentManagedThreadId,
                    Timestamp = DateTimeOffset.UtcNow
                };

                // Log slow operations
                if (elapsedMs > _options!.SlowOperationThresholdMs)
                {
                    _logger?.LogWarning("Slow background service operation detected: {ServiceName}.{OperationName} took {ElapsedMs}ms", 
                        serviceName, operationName, elapsedMs);
                }

                _appLogger!.Log(
                    "{LogType} {ServiceName} {OperationName} {OperationId} {ElapsedMs} Completed {LogData}",
                    LogType.BackgroundService.ToString(),
                    serviceName,
                    operationName,
                    operationId,
                    elapsedMs,
                    Newtonsoft.Json.JsonConvert.SerializeObject(logData, Newtonsoft.Json.Formatting.None)
                );
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error logging background service operation completion");
            }

            await Task.CompletedTask;
        }

        private static async Task LogBackgroundOperationErrorAsync(
            string serviceName,
            string operationName,
            string operationId,
            long elapsedMs,
            Exception exception)
        {
            try
            {
                var logData = new
                {
                    ServiceName = serviceName,
                    OperationName = operationName,
                    OperationId = operationId,
                    ElapsedMilliseconds = elapsedMs,
                    Success = false,
                    Error = exception.Message,
                    ExceptionType = exception.GetType().Name,
                    StackTrace = exception.StackTrace?.Substring(0, Math.Min(exception.StackTrace.Length, 2000)),
                    ThreadId = Environment.CurrentManagedThreadId,
                    Timestamp = DateTimeOffset.UtcNow
                };

                _appLogger!.Log(
                    "{LogType} {ServiceName} {OperationName} {OperationId} {Error} Failed {LogData}",
                    LogType.BackgroundService.ToString(),
                    serviceName,
                    operationName,
                    operationId,
                    exception.Message,
                    Newtonsoft.Json.JsonConvert.SerializeObject(logData, Newtonsoft.Json.Formatting.None)
                );
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error logging background service operation error");
            }

            await Task.CompletedTask;
        }

        private static async Task LogBackgroundOperationCancelledAsync(
            string serviceName,
            string operationName,
            string operationId,
            long elapsedMs)
        {
            try
            {
                var logData = new
                {
                    ServiceName = serviceName,
                    OperationName = operationName,
                    OperationId = operationId,
                    ElapsedMilliseconds = elapsedMs,
                    Success = false,
                    Cancelled = true,
                    ThreadId = Environment.CurrentManagedThreadId,
                    Timestamp = DateTimeOffset.UtcNow
                };

                _appLogger!.Log(
                    "{LogType} {ServiceName} {OperationName} {OperationId} Cancelled {LogData}",
                    LogType.BackgroundService.ToString(),
                    serviceName,
                    operationName,
                    operationId,
                    Newtonsoft.Json.JsonConvert.SerializeObject(logData, Newtonsoft.Json.Formatting.None)
                );
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error logging background service operation cancellation");
            }

            await Task.CompletedTask;
        }
    }

    public class BackgroundServiceInstrumentationOptions
    {
        public bool LogOperations { get; set; } = true;
        public bool LogResults { get; set; } = true;
        public bool LogErrors { get; set; } = true;
        public bool LogCancellations { get; set; } = true;
        public double SlowOperationThresholdMs { get; set; } = 5000; // 5 seconds for background operations
    }

    // Base class for instrumented background services
    public abstract class InstrumentedBackgroundService : BackgroundService
    {
        protected readonly IAppLogger _appLogger;
        protected readonly ILogger _logger;
        protected readonly string _serviceName;

        protected InstrumentedBackgroundService(IAppLogger appLogger, ILogger logger)
        {
            _appLogger = appLogger;
            _logger = logger;
            _serviceName = GetType().Name;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await BackgroundServiceInstrumentation.LogBackgroundOperationAsync(
                _serviceName,
                "Execute",
                () => ExecuteServiceAsync(stoppingToken),
                stoppingToken);
        }

        protected abstract Task ExecuteServiceAsync(CancellationToken stoppingToken);

        protected async Task LogOperationAsync(string operationName, Func<Task> operation)
        {
            await BackgroundServiceInstrumentation.LogBackgroundOperationAsync(_serviceName, operationName, operation);
        }

        protected async Task<T> LogOperationAsync<T>(string operationName, Func<Task<T>> operation)
        {
            return await BackgroundServiceInstrumentation.LogBackgroundOperationAsync(_serviceName, operationName, operation);
        }
    }
}