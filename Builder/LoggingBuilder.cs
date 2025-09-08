using Framework.Core.Logging.Options;
using Framework.Core.Logging.Instrumentation;
using Framework.Core.Logging.Logging.AsyncLogging;
using Framework.Core.Logging.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Framework.Core.Logging.Builder
{
    /// <summary>
    /// Fluent API builder implementation for Framework Core Logging
    /// </summary>
    internal class LoggingBuilder : IFrameworkLoggingBuilder
    {
        public IServiceCollection Services { get; }
        public LoggingOptions Options { get; }

        public LoggingBuilder(IServiceCollection services, LoggingOptions options)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        // HTTP Logging Configuration

        public IFrameworkLoggingBuilder EnableHttpLogging(bool enabled = true)
        {
            Options.HttpLogging.Enabled = enabled;
            return this;
        }

        public IFrameworkLoggingBuilder LogHeaders(bool enabled = true)
        {
            Options.HttpLogging.LogHeaders = enabled;
            return this;
        }

        public IFrameworkLoggingBuilder LogBody(bool enabled = true)
        {
            Options.HttpLogging.LogBody = enabled;
            return this;
        }

        public IFrameworkLoggingBuilder SetMaxContentLength(int maxLength)
        {
            if (maxLength <= 0)
                throw new ArgumentException("Max content length must be greater than 0", nameof(maxLength));

            Options.HttpLogging.MaxContentLength = maxLength;
            return this;
        }

        public IFrameworkLoggingBuilder ExcludePaths(params string[] paths)
        {
            if (paths == null) return this;

            var existingPaths = Options.HttpLogging.ExcludedPaths?.ToList() ?? new List<string>();
            existingPaths.AddRange(paths.Where(p => !string.IsNullOrWhiteSpace(p)));
            Options.HttpLogging.ExcludedPaths = existingPaths.Distinct().ToArray();
            return this;
        }

        public IFrameworkLoggingBuilder MaskSensitiveFields(params string[] fields)
        {
            if (fields == null) return this;

            var existingFields = Options.HttpLogging.SensitiveFields?.ToList() ?? new List<string>();
            existingFields.AddRange(fields.Where(f => !string.IsNullOrWhiteSpace(f)));
            Options.HttpLogging.SensitiveFields = existingFields.Distinct().ToArray();
            return this;
        }

        public IFrameworkLoggingBuilder MaskSensitiveHeaders(params string[] headers)
        {
            if (headers == null) return this;

            var existingHeaders = Options.HttpLogging.SensitiveHeaders?.ToList() ?? new List<string>();
            existingHeaders.AddRange(headers.Where(h => !string.IsNullOrWhiteSpace(h)));
            Options.HttpLogging.SensitiveHeaders = existingHeaders.Distinct().ToArray();
            return this;
        }

        // Method Logging Configuration

        public IFrameworkLoggingBuilder EnableMethodLogging(bool enabled = true)
        {
            Options.MethodLogging.Enabled = enabled;
            return this;
        }

        public IFrameworkLoggingBuilder LogMethodParameters(bool enabled = true)
        {
            Options.MethodLogging.LogParameters = enabled;
            return this;
        }

        public IFrameworkLoggingBuilder LogMethodReturnValues(bool enabled = true)
        {
            Options.MethodLogging.LogReturnValues = enabled;
            return this;
        }

        public IFrameworkLoggingBuilder LogExecutionTime(bool enabled = true)
        {
            Options.MethodLogging.LogExecutionTime = enabled;
            return this;
        }

        public IFrameworkLoggingBuilder SetMinimumExecutionTime(int milliseconds)
        {
            if (milliseconds < 0)
                throw new ArgumentException("Minimum execution time cannot be negative", nameof(milliseconds));

            Options.MethodLogging.MinimumExecutionTimeMs = milliseconds;
            return this;
        }

        // Correlation ID Configuration

        public IFrameworkLoggingBuilder WithCorrelationId(bool enabled = true)
        {
            Options.CorrelationId.Enabled = enabled;
            return this;
        }

        public IFrameworkLoggingBuilder SetCorrelationIdHeader(string headerName)
        {
            if (string.IsNullOrWhiteSpace(headerName))
                throw new ArgumentException("Header name cannot be null or empty", nameof(headerName));

            Options.CorrelationId.HeaderName = headerName;
            return this;
        }

        public IFrameworkLoggingBuilder GenerateCorrelationIdIfMissing(bool enabled = true)
        {
            Options.CorrelationId.GenerateIfMissing = enabled;
            return this;
        }

        public IFrameworkLoggingBuilder IncludeCorrelationIdInResponse(bool enabled = true)
        {
            Options.CorrelationId.IncludeInResponse = enabled;
            return this;
        }

        // General Configuration

        public IFrameworkLoggingBuilder SetApplicationName(string applicationName)
        {
            if (string.IsNullOrWhiteSpace(applicationName))
                throw new ArgumentException("Application name cannot be null or empty", nameof(applicationName));

            Options.ApplicationName = applicationName;
            return this;
        }

        public IFrameworkLoggingBuilder EnableConsoleLogging(bool enabled = true)
        {
            Options.ConsoleEnabled = enabled;
            return this;
        }

        public IFrameworkLoggingBuilder EnableDebugMode(bool enabled = true)
        {
            Options.DebugMode = enabled;
            return this;
        }

        public IFrameworkLoggingBuilder SetLogLevel(LogLevel logLevel)
        {
            Options.LogLevel = logLevel;
            return this;
        }

        // Advanced Configuration

        public IFrameworkLoggingBuilder ConfigureHttpLogging(Action<HttpLoggingOptions> configure)
        {
            configure?.Invoke(Options.HttpLogging);
            return this;
        }

        public IFrameworkLoggingBuilder ConfigureMethodLogging(Action<MethodLoggingOptions> configure)
        {
            configure?.Invoke(Options.MethodLogging);
            return this;
        }

        public IFrameworkLoggingBuilder ConfigureCorrelationId(Action<CorrelationIdOptions> configure)
        {
            configure?.Invoke(Options.CorrelationId);
            return this;
        }

        public IFrameworkLoggingBuilder Configure(Action<LoggingOptions> configure)
        {
            configure?.Invoke(Options);
            return this;
        }

        // Global Exception Handling Configuration

        public IFrameworkLoggingBuilder EnableGlobalExceptionHandling(bool enabled = true)
        {
            if (enabled)
            {
                Services.Configure<GlobalExceptionOptions>(opt =>
                {
                    opt.ModifyResponse = false;
                    opt.IncludeStackTrace = false;
                    opt.LogRequestBody = true;
                    opt.EnableMetrics = true;
                });
            }
            return this;
        }

        public IFrameworkLoggingBuilder ConfigureGlobalExceptionHandling(Action<GlobalExceptionOptions> configure)
        {
            Services.Configure<GlobalExceptionOptions>(configure);
            return this;
        }

        // Async Logging Configuration

        public IFrameworkLoggingBuilder EnableAsyncLogging(bool enabled = true)
        {
            if (enabled)
            {
                Services.Configure<AsyncLoggingOptions>(opt =>
                {
                    opt.QueueCapacity = 10000;
                    opt.BatchSize = 50;
                    opt.FlushInterval = TimeSpan.FromMilliseconds(100);
                    opt.EnableObjectPooling = true;
                    opt.MaxConcurrentWrites = Environment.ProcessorCount;
                });
            }
            return this;
        }

        public IFrameworkLoggingBuilder ConfigureAsyncLogging(Action<AsyncLoggingOptions> configure)
        {
            Services.Configure<AsyncLoggingOptions>(configure);
            return this;
        }

        // Auto-Instrumentation Configuration

        public IFrameworkLoggingBuilder EnableAutoInstrumentation(bool enabled = true)
        {
            if (enabled)
            {
                Services.Configure<AutoInstrumentationOptions>(opt =>
                {
                    opt.EnableDatabaseInstrumentation = true;
                    opt.EnableRedisInstrumentation = true;
                    opt.EnableBackgroundServiceInstrumentation = true;
                    opt.EnableHttpClientInstrumentation = true;
                });
            }
            return this;
        }

        public IFrameworkLoggingBuilder ConfigureAutoInstrumentation(Action<AutoInstrumentationOptions> configure)
        {
            Services.Configure<AutoInstrumentationOptions>(configure);
            return this;
        }

        public IFrameworkLoggingBuilder EnableDatabaseInstrumentation(bool enabled = true)
        {
            Services.Configure<AutoInstrumentationOptions>(opt =>
                opt.EnableDatabaseInstrumentation = enabled);
            return this;
        }

        public IFrameworkLoggingBuilder ConfigureDatabaseInstrumentation(Action<DatabaseInstrumentationOptions> configure)
        {
            Services.Configure<AutoInstrumentationOptions>(opt =>
                configure?.Invoke(opt.Database));
            return this;
        }

        public IFrameworkLoggingBuilder EnableRedisInstrumentation(bool enabled = true)
        {
            Services.Configure<AutoInstrumentationOptions>(opt =>
                opt.EnableRedisInstrumentation = enabled);
            return this;
        }

        public IFrameworkLoggingBuilder ConfigureRedisInstrumentation(Action<RedisInstrumentationOptions> configure)
        {
            Services.Configure<AutoInstrumentationOptions>(opt =>
                configure?.Invoke(opt.Redis));
            return this;
        }

        public IFrameworkLoggingBuilder EnableBackgroundServiceInstrumentation(bool enabled = true)
        {
            Services.Configure<AutoInstrumentationOptions>(opt =>
                opt.EnableBackgroundServiceInstrumentation = enabled);
            return this;
        }

        public IFrameworkLoggingBuilder ConfigureBackgroundServiceInstrumentation(Action<BackgroundServiceInstrumentationOptions> configure)
        {
            Services.Configure<AutoInstrumentationOptions>(opt =>
                configure?.Invoke(opt.BackgroundService));
            return this;
        }

        // Memory Optimization Configuration

        public IFrameworkLoggingBuilder OptimizeMemoryUsage(bool enabled = true)
        {
            if (enabled)
            {
                // Enable object pooling
                Services.Configure<AsyncLoggingOptions>(opt =>
                    opt.EnableObjectPooling = true);

                // Configure smaller batch sizes for memory efficiency
                Services.Configure<AsyncLoggingOptions>(opt =>
                    opt.BatchSize = 25);
            }
            return this;
        }

        // Security Configuration

        public IFrameworkLoggingBuilder EnableSecurityFeatures(bool enabled = true)
        {
            if (enabled)
            {
                // Enhanced sensitive data masking
                MaskSensitiveFields("password", "token", "secret", "key", "auth", "credential", "pin", "ssn");
                MaskSensitiveHeaders("Authorization", "Cookie", "Set-Cookie", "X-API-Key", "X-Auth-Token");

                // Configure auto-instrumentation security
                Services.Configure<AutoInstrumentationOptions>(opt =>
                {
                    opt.Database.LogSqlParameters = false;
                    opt.Redis.LogValues = false;
                });
            }
            return this;
        }
    }
}