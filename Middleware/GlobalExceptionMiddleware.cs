using Framework.Core.Logging.Helper;
using Framework.Core.Logging.Logging.AppLogger;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;

namespace Framework.Core.Logging.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAppLogger _appLogger;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly GlobalExceptionOptions _options;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            IAppLogger appLogger,
            ILogger<GlobalExceptionMiddleware> logger,
            GlobalExceptionOptions options)
        {
            _next = next;
            _appLogger = appLogger;
            _logger = logger;
            _options = options;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
                throw; // Re-throw to maintain normal exception flow
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            try
            {
                var correlationId = context.TraceIdentifier;
                var exceptionInfo = ClassifyException(exception);

                // Log exception with full context
                await LogExceptionAsync(exception, context, correlationId, exceptionInfo);

                // Optionally set response (if response hasn't started)
                if (!context.Response.HasStarted && _options.ModifyResponse)
                {
                    await SetErrorResponseAsync(context, exceptionInfo, correlationId);
                }
            }
            catch (Exception logEx)
            {
                // Fallback logging to prevent exception loop
                _logger.LogCritical(logEx, "Failed to log exception in GlobalExceptionMiddleware");
            }
        }

        private async Task LogExceptionAsync(
            Exception exception,
            HttpContext context,
            string correlationId,
            ExceptionInfo exceptionInfo)
        {
            var exceptionData = new
            {
                CorrelationId = correlationId,
                ExceptionType = exception.GetType().FullName,
                Message = exception.Message,
                StackTrace = exception.StackTrace?.Crop(100 * 1024),
                InnerException = exception.InnerException?.GetType().FullName,
                InnerExceptionMessage = exception.InnerException?.Message,

                // Request context
                RequestPath = context.Request.Path.Value,
                RequestMethod = context.Request.Method,
                QueryString = context.Request.QueryString.Value,
                UserAgent = context.Request.Headers["User-Agent"].ToString(),
                RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString(),

                // User context
                UserId = context.User?.FindFirst("sub")?.Value ?? context.User?.FindFirst("id")?.Value,
                UserName = context.User?.FindFirst("name")?.Value,

                // Classification
                ExceptionCategory = exceptionInfo.Category.ToString(),
                Severity = exceptionInfo.LogLevel.ToString(),
                ShouldAlert = exceptionInfo.ShouldNotify,

                // Environment context
                MachineName = Environment.MachineName,
                ProcessId = Environment.ProcessId,
                ThreadId = Environment.CurrentManagedThreadId,

                Timestamp = DateTimeOffset.UtcNow
            };

            var logData = JsonConvert.SerializeObject(exceptionData, Formatting.None);

            _appLogger.Log(
                "{LogType} {ExceptionType} {RequestPath} {CorrelationId} {LogData}",
                LogType.Exception.ToString(),
                exception.GetType().Name,
                context.Request.Path.Value,
                correlationId,
                logData
            );

            // Also use built-in logger for structured logging
            _logger.Log(exceptionInfo.LogLevel, exception,
                "Global exception: {ExceptionType} at {RequestPath} | CorrelationId: {CorrelationId}",
                exception.GetType().Name, context.Request.Path, correlationId);
        }

        private async Task SetErrorResponseAsync(HttpContext context, ExceptionInfo exceptionInfo, string correlationId)
        {
            context.Response.StatusCode = (int)exceptionInfo.StatusCode;
            context.Response.ContentType = "application/json";

            var response = new
            {
                Error = exceptionInfo.UserMessage,
                CorrelationId = correlationId,
                Timestamp = DateTimeOffset.UtcNow,
                Details = _options.IncludeStackTrace ? exceptionInfo.Details : null
            };

            var json = JsonConvert.SerializeObject(response, Formatting.Indented);
            await context.Response.WriteAsync(json);
        }

        private ExceptionInfo ClassifyException(Exception exception)
        {
            return exception switch
            {
                ArgumentException or ArgumentNullException => new ExceptionInfo
                {
                    Category = ExceptionCategory.Validation,
                    StatusCode = HttpStatusCode.BadRequest,
                    UserMessage = "Invalid request parameters",
                    LogLevel = LogLevel.Warning,
                    ShouldNotify = false
                },

                UnauthorizedAccessException => new ExceptionInfo
                {
                    Category = ExceptionCategory.Authorization,
                    StatusCode = HttpStatusCode.Unauthorized,
                    UserMessage = "Unauthorized access",
                    LogLevel = LogLevel.Warning,
                    ShouldNotify = false
                },

                KeyNotFoundException => new ExceptionInfo
                {
                    Category = ExceptionCategory.NotFound,
                    StatusCode = HttpStatusCode.NotFound,
                    UserMessage = "Resource not found",
                    LogLevel = LogLevel.Information,
                    ShouldNotify = false
                },

                TimeoutException => new ExceptionInfo
                {
                    Category = ExceptionCategory.Timeout,
                    StatusCode = HttpStatusCode.RequestTimeout,
                    UserMessage = "Request timeout",
                    LogLevel = LogLevel.Error,
                    ShouldNotify = true
                },

                HttpRequestException => new ExceptionInfo
                {
                    Category = ExceptionCategory.ExternalService,
                    StatusCode = HttpStatusCode.BadGateway,
                    UserMessage = "External service error",
                    LogLevel = LogLevel.Error,
                    ShouldNotify = true
                },

                InvalidOperationException when exception.Message.Contains("database") => new ExceptionInfo
                {
                    Category = ExceptionCategory.Database,
                    StatusCode = HttpStatusCode.InternalServerError,
                    UserMessage = "Database operation failed",
                    LogLevel = LogLevel.Error,
                    ShouldNotify = true
                },

                _ => new ExceptionInfo
                {
                    Category = ExceptionCategory.System,
                    StatusCode = HttpStatusCode.InternalServerError,
                    UserMessage = "An unexpected error occurred",
                    LogLevel = LogLevel.Critical,
                    ShouldNotify = true,
                    Details = _options.IncludeStackTrace ? exception.StackTrace : null
                }
            };
        }
    }

    public class ExceptionInfo
    {
        public ExceptionCategory Category { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string UserMessage { get; set; } = string.Empty;
        public LogLevel LogLevel { get; set; }
        public bool ShouldNotify { get; set; }
        public object? Details { get; set; }
    }

    public enum ExceptionCategory
    {
        Validation,
        Authorization,
        NotFound,
        Timeout,
        ExternalService,
        Database,
        System
    }

    public class GlobalExceptionOptions
    {
        public bool ModifyResponse { get; set; } = false;
        public bool IncludeStackTrace { get; set; } = false;
        public bool LogRequestBody { get; set; } = true;
        public bool EnableMetrics { get; set; } = true;
        public string[] SensitiveHeaders { get; set; } = { "Authorization", "Cookie", "Set-Cookie" };
    }
}