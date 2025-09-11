using Framework.Core.Logging.Helper;
using Framework.Core.Logging.Logging.AppLogger;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using System.Text.Json;
using System.Threading.Channels;

namespace Framework.Core.Logging.Logging.AsyncLogging
{
    public class AsyncLogger : IAsyncLogger, IAsyncDisposable, IDisposable
    {
        private readonly Channel<LogEntry> _channel;
        private readonly ChannelWriter<LogEntry> _writer;
        private readonly ChannelReader<LogEntry> _reader;
        private readonly IAppLogger _appLogger;
        private static readonly ICorrelationIdHelper _correlationIdHelper = new CorrelationIdHelper();
        private readonly PlatformAppLoggerConfiguration _config;
        private readonly ObjectPool<LogEntry> _logEntryPool;
        private readonly Task _backgroundTask;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _disposed;

        public AsyncLogger(
            IAppLogger appLogger,
            PlatformAppLoggerConfiguration config,
            AsyncLoggingOptions options)
        {
            _appLogger = appLogger;
            _config = config;

            // Create bounded channel for log entries
            var channelOptions = new BoundedChannelOptions(options.QueueCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            };

            _channel = Channel.CreateBounded<LogEntry>(channelOptions);
            _writer = _channel.Writer;
            _reader = _channel.Reader;

            // Object pool for LogEntry instances
            _logEntryPool = new DefaultObjectPool<LogEntry>(new LogEntryPooledObjectPolicy());

            // Background processing
            _cancellationTokenSource = new CancellationTokenSource();
            _backgroundTask = ProcessLogEntriesAsync(_cancellationTokenSource.Token);
        }

        public async ValueTask LogAsync(string messageTemplate, params object?[]? propertyValues)
        {
            await LogAsync(LogLevel.Information, null, messageTemplate, propertyValues);
        }

        public async ValueTask LogAsync(LogLevel level, string messageTemplate, params object?[]? propertyValues)
        {
            await LogAsync(level, null, messageTemplate, propertyValues);
        }

        public async ValueTask LogAsync(LogLevel level, Exception? exception, string messageTemplate, params object?[]? propertyValues)
        {
            if (_disposed) return;

            var logEntry = _logEntryPool.Get();
            try
            {
                logEntry.Level = level;
                logEntry.MessageTemplate = messageTemplate;
                logEntry.PropertyValues = propertyValues;
                logEntry.Exception = exception;
                logEntry.Timestamp = DateTimeOffset.UtcNow;
                logEntry.CorrelationId = _correlationIdHelper.Get();
                logEntry.MachineName = PlatformAppLoggerConfiguration.MachineName;
                logEntry.ApplicationName = _config.LoggerApplicationName;

                await _writer.WriteAsync(logEntry, _cancellationTokenSource.Token);
            }
            catch (InvalidOperationException)
            {
                // Channel is closed, return entry to pool
                _logEntryPool.Return(logEntry);
            }
            catch (OperationCanceledException)
            {
                // Cancellation requested, return entry to pool
                _logEntryPool.Return(logEntry);
            }
        }

        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            // Signal no more writes and wait for processing to complete
            _writer.Complete();
            
            try
            {
                await _backgroundTask.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
        }

        private async Task ProcessLogEntriesAsync(CancellationToken cancellationToken)
        {
            var batch = new List<LogEntry>();
            var batchTimeout = TimeSpan.FromMilliseconds(100); // 100ms batch timeout

            try
            {
                await foreach (var logEntry in _reader.ReadAllAsync(cancellationToken))
                {
                    batch.Add(logEntry);

                    // Process batch when it's full or timeout reached
                    if (batch.Count >= 50) // Batch size
                    {
                        await ProcessBatchAsync(batch);
                        batch.Clear();
                    }
                }

                // Process remaining entries
                if (batch.Count > 0)
                {
                    await ProcessBatchAsync(batch);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown, process remaining entries
                if (batch.Count > 0)
                {
                    await ProcessBatchAsync(batch);
                }
            }
            catch (Exception ex)
            {
                // Log processing error (fallback to console to avoid infinite loop)
                Console.WriteLine($"Error in async log processing: {ex}");
            }
        }

        private async Task ProcessBatchAsync(List<LogEntry> batch)
        {
            foreach (var logEntry in batch)
            {
                try
                {
                    await ProcessSingleLogAsync(logEntry);
                }
                catch (Exception ex)
                {
                    // Log individual entry error (fallback to console)
                    Console.WriteLine($"Error processing log entry: {ex}");
                }
                finally
                {
                    // Return entry to pool
                    _logEntryPool.Return(logEntry);
                }
            }
        }

        private async Task ProcessSingleLogAsync(LogEntry logEntry)
        {
            if (logEntry.Exception != null)
            {
                // Use existing exception logging
                _appLogger.Exception(logEntry.Exception, 
                    System.Reflection.MethodBase.GetCurrentMethod()!, 
                    logEntry.MessageTemplate);
            }
            else
            {
                // Use existing structured logging
                _appLogger.Log(logEntry.MessageTemplate, logEntry.PropertyValues);
            }

            await Task.CompletedTask; // Placeholder for any async operations
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;

            try
            {
                // Complete the channel (no more writes)
                _writer.Complete();

                // Wait for background processing to complete
                await _backgroundTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during AsyncLogger disposal: {ex}");
            }
            finally
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _disposed = true;
            }

            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    public class LogEntryPooledObjectPolicy : PooledObjectPolicy<LogEntry>
    {
        public override LogEntry Create() => new LogEntry();

        public override bool Return(LogEntry obj)
        {
            obj.Reset();
            return true;
        }
    }

    public class AsyncLoggingOptions
    {
        public int QueueCapacity { get; set; } = 10000;
        public int BatchSize { get; set; } = 50;
        public TimeSpan FlushInterval { get; set; } = TimeSpan.FromMilliseconds(100);
        public bool EnableObjectPooling { get; set; } = true;
        public int MaxConcurrentWrites { get; set; } = Environment.ProcessorCount;
    }
}