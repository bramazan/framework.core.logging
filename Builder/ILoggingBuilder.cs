using Framework.Core.Logging.Options;
using Framework.Core.Logging.Instrumentation;
using Framework.Core.Logging.Logging.AsyncLogging;
using Framework.Core.Logging.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Framework.Core.Logging.Builder
{
    /// <summary>
    /// Fluent API builder for configuring Framework Core Logging
    /// </summary>
    public interface IFrameworkLoggingBuilder
    {
        /// <summary>
        /// Service collection for registration
        /// </summary>
        IServiceCollection Services { get; }

        /// <summary>
        /// Logging options being configured
        /// </summary>
        LoggingOptions Options { get; }

        // HTTP Logging Configuration
        
        /// <summary>
        /// Enable HTTP request/response logging
        /// </summary>
        IFrameworkLoggingBuilder EnableHttpLogging(bool enabled = true);

        /// <summary>
        /// Configure HTTP logging to include headers
        /// </summary>
        IFrameworkLoggingBuilder LogHeaders(bool enabled = true);

        /// <summary>
        /// Configure HTTP logging to include request/response body
        /// </summary>
        IFrameworkLoggingBuilder LogBody(bool enabled = true);

        /// <summary>
        /// Set maximum content length for HTTP logging
        /// </summary>
        IFrameworkLoggingBuilder SetMaxContentLength(int maxLength);

        /// <summary>
        /// Add paths to exclude from HTTP logging
        /// </summary>
        IFrameworkLoggingBuilder ExcludePaths(params string[] paths);

        /// <summary>
        /// Add sensitive fields to mask in logs
        /// </summary>
        IFrameworkLoggingBuilder MaskSensitiveFields(params string[] fields);

        /// <summary>
        /// Add sensitive headers to exclude from logging
        /// </summary>
        IFrameworkLoggingBuilder MaskSensitiveHeaders(params string[] headers);

        // Method Logging Configuration

        /// <summary>
        /// Enable method entry/exit logging
        /// </summary>
        IFrameworkLoggingBuilder EnableMethodLogging(bool enabled = true);

        /// <summary>
        /// Configure method logging to include parameters
        /// </summary>
        IFrameworkLoggingBuilder LogMethodParameters(bool enabled = true);

        /// <summary>
        /// Configure method logging to include return values
        /// </summary>
        IFrameworkLoggingBuilder LogMethodReturnValues(bool enabled = true);

        /// <summary>
        /// Configure method logging to include execution time
        /// </summary>
        IFrameworkLoggingBuilder LogExecutionTime(bool enabled = true);

        /// <summary>
        /// Set minimum execution time to log (in milliseconds)
        /// </summary>
        IFrameworkLoggingBuilder SetMinimumExecutionTime(int milliseconds);

        // Correlation ID Configuration

        /// <summary>
        /// Enable correlation ID tracking
        /// </summary>
        IFrameworkLoggingBuilder WithCorrelationId(bool enabled = true);

        /// <summary>
        /// Set correlation ID header name
        /// </summary>
        IFrameworkLoggingBuilder SetCorrelationIdHeader(string headerName);

        /// <summary>
        /// Configure whether to generate correlation ID if missing
        /// </summary>
        IFrameworkLoggingBuilder GenerateCorrelationIdIfMissing(bool enabled = true);

        /// <summary>
        /// Configure whether to include correlation ID in response headers
        /// </summary>
        IFrameworkLoggingBuilder IncludeCorrelationIdInResponse(bool enabled = true);

        // General Configuration

        /// <summary>
        /// Set application name for logging
        /// </summary>
        IFrameworkLoggingBuilder SetApplicationName(string applicationName);

        /// <summary>
        /// Enable console logging
        /// </summary>
        IFrameworkLoggingBuilder EnableConsoleLogging(bool enabled = true);

        /// <summary>
        /// Enable debug mode
        /// </summary>
        IFrameworkLoggingBuilder EnableDebugMode(bool enabled = true);

        /// <summary>
        /// Set minimum log level
        /// </summary>
        IFrameworkLoggingBuilder SetLogLevel(LogLevel logLevel);

        // Advanced Configuration

        /// <summary>
        /// Configure HTTP logging options using a delegate
        /// </summary>
        IFrameworkLoggingBuilder ConfigureHttpLogging(Action<HttpLoggingOptions> configure);

        /// <summary>
        /// Configure method logging options using a delegate
        /// </summary>
        IFrameworkLoggingBuilder ConfigureMethodLogging(Action<MethodLoggingOptions> configure);

        /// <summary>
        /// Configure correlation ID options using a delegate
        /// </summary>
        IFrameworkLoggingBuilder ConfigureCorrelationId(Action<CorrelationIdOptions> configure);

        /// <summary>
        /// Configure all options using a delegate
        /// </summary>
        IFrameworkLoggingBuilder Configure(Action<LoggingOptions> configure);

        // Global Exception Handling Configuration

        /// <summary>
        /// Enable global exception handling middleware
        /// </summary>
        IFrameworkLoggingBuilder EnableGlobalExceptionHandling(bool enabled = true);

        /// <summary>
        /// Configure global exception handling options using a delegate
        /// </summary>
        IFrameworkLoggingBuilder ConfigureGlobalExceptionHandling(Action<GlobalExceptionOptions> configure);

        // Async Logging Configuration

        /// <summary>
        /// Enable asynchronous logging with background processing
        /// </summary>
        IFrameworkLoggingBuilder EnableAsyncLogging(bool enabled = true);

        /// <summary>
        /// Configure async logging options using a delegate
        /// </summary>
        IFrameworkLoggingBuilder ConfigureAsyncLogging(Action<AsyncLoggingOptions> configure);

        // Auto-Instrumentation Configuration

        /// <summary>
        /// Enable automatic instrumentation for all supported components
        /// </summary>
        IFrameworkLoggingBuilder EnableAutoInstrumentation(bool enabled = true);

        /// <summary>
        /// Configure auto-instrumentation options using a delegate
        /// </summary>
        IFrameworkLoggingBuilder ConfigureAutoInstrumentation(Action<AutoInstrumentationOptions> configure);

        /// <summary>
        /// Enable database operations instrumentation
        /// </summary>
        IFrameworkLoggingBuilder EnableDatabaseInstrumentation(bool enabled = true);

        /// <summary>
        /// Configure database instrumentation options using a delegate
        /// </summary>
        IFrameworkLoggingBuilder ConfigureDatabaseInstrumentation(Action<DatabaseInstrumentationOptions> configure);

        /// <summary>
        /// Enable Redis operations instrumentation
        /// </summary>
        IFrameworkLoggingBuilder EnableRedisInstrumentation(bool enabled = true);

        /// <summary>
        /// Configure Redis instrumentation options using a delegate
        /// </summary>
        IFrameworkLoggingBuilder ConfigureRedisInstrumentation(Action<RedisInstrumentationOptions> configure);

        /// <summary>
        /// Enable background service operations instrumentation
        /// </summary>
        IFrameworkLoggingBuilder EnableBackgroundServiceInstrumentation(bool enabled = true);

        /// <summary>
        /// Configure background service instrumentation options using a delegate
        /// </summary>
        IFrameworkLoggingBuilder ConfigureBackgroundServiceInstrumentation(Action<BackgroundServiceInstrumentationOptions> configure);

        // Performance & Security Configuration

        /// <summary>
        /// Enable memory optimization features (object pooling, batch processing)
        /// </summary>
        IFrameworkLoggingBuilder OptimizeMemoryUsage(bool enabled = true);

        /// <summary>
        /// Enable enhanced security features (advanced data masking, secure defaults)
        /// </summary>
        IFrameworkLoggingBuilder EnableSecurityFeatures(bool enabled = true);
    }
}