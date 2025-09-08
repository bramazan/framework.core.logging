using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace Framework.Core.Logging.Options
{
    /// <summary>
    /// Modern logging options with validation support
    /// </summary>
    public class LoggingOptions
    {
        /// <summary>
        /// Application name for logging
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string ApplicationName { get; set; } = Environment.MachineName ?? "UnknownApp";

        /// <summary>
        /// Enable console logging
        /// </summary>
        public bool ConsoleEnabled { get; set; } = true;

        /// <summary>
        /// Enable debug mode
        /// </summary>
        public bool DebugMode { get; set; } = false;

        /// <summary>
        /// Minimum log level
        /// </summary>
        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// HTTP logging configuration
        /// </summary>
        public HttpLoggingOptions HttpLogging { get; set; } = new();

        /// <summary>
        /// Method logging configuration
        /// </summary>
        public MethodLoggingOptions MethodLogging { get; set; } = new();

        /// <summary>
        /// Correlation ID configuration
        /// </summary>
        public CorrelationIdOptions CorrelationId { get; set; } = new();
    }

    /// <summary>
    /// HTTP logging specific options
    /// </summary>
    public class HttpLoggingOptions
    {
        /// <summary>
        /// Enable HTTP request/response logging
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Log HTTP headers
        /// </summary>
        public bool LogHeaders { get; set; } = true;

        /// <summary>
        /// Log HTTP body
        /// </summary>
        public bool LogBody { get; set; } = true;

        /// <summary>
        /// Maximum content length to log
        /// </summary>
        [Range(1, 10 * 1024 * 1024)] // 1 byte to 10MB
        public int MaxContentLength { get; set; } = 4096;

        /// <summary>
        /// Paths to exclude from logging
        /// </summary>
        public string[] ExcludedPaths { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Sensitive fields to mask in logs
        /// </summary>
        public string[] SensitiveFields { get; set; } = new[] { "password", "token", "secret", "key" };

        /// <summary>
        /// Sensitive headers to exclude from logging
        /// </summary>
        public string[] SensitiveHeaders { get; set; } = new[] { "Authorization", "Cookie", "Set-Cookie" };

        /// <summary>
        /// Enable method entry/exit logging
        /// </summary>
        public bool LogMethodEntryExit { get; set; } = true;

        /// <summary>
        /// Maximum body size for logging (in bytes)
        /// </summary>
        [Range(1, 1024 * 1024)] // 1 byte to 1MB
        public int MaxBodySize { get; set; } = 64 * 1024; // 64KB
    }

    /// <summary>
    /// Method logging specific options
    /// </summary>
    public class MethodLoggingOptions
    {
        /// <summary>
        /// Enable method logging
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Log method parameters
        /// </summary>
        public bool LogParameters { get; set; } = true;

        /// <summary>
        /// Log method return values
        /// </summary>
        public bool LogReturnValues { get; set; } = false;

        /// <summary>
        /// Log execution time
        /// </summary>
        public bool LogExecutionTime { get; set; } = true;

        /// <summary>
        /// Minimum execution time to log (in milliseconds)
        /// </summary>
        [Range(0, 60000)] // 0 to 60 seconds
        public int MinimumExecutionTimeMs { get; set; } = 0;
    }

    /// <summary>
    /// Correlation ID specific options
    /// </summary>
    public class CorrelationIdOptions
    {
        /// <summary>
        /// Enable correlation ID tracking
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Header name for correlation ID
        /// </summary>
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string HeaderName { get; set; } = "X-Correlation-Id";

        /// <summary>
        /// Generate new correlation ID if not present in request
        /// </summary>
        public bool GenerateIfMissing { get; set; } = true;

        /// <summary>
        /// Include correlation ID in response headers
        /// </summary>
        public bool IncludeInResponse { get; set; } = true;
    }
}