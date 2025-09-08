using Framework.Core.Logging.Helper;
using Framework.Core.Logging.Logging.AppLogger;
using Microsoft.AspNetCore.Http;
using Microsoft.IO;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Core.Logging.Middleware
{
    public class HttpLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAppLogger _appLogger;
        private readonly PlatformAppLoggerConfiguration _config;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public HttpLoggingMiddleware(RequestDelegate next, IAppLogger appLogger, PlatformAppLoggerConfiguration config)
        {
            _next = next;
            _appLogger = appLogger;
            _config = config;
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!_config.HttpLogging.LogRequests && !_config.HttpLogging.LogResponses)
            {
                await _next(context);
                return;
            }

            // Check if path should be ignored
            if (context.Request.Path.Value.ShouldIgnorePath(_config.HttpLogging.IgnoredPaths))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var correlationId = context.TraceIdentifier;

            try
            {
                // Log request if enabled
                if (_config.HttpLogging.LogRequests)
                {
                    await LogHttpRequest(context, correlationId);
                }

                // Replace response stream to capture response
                var originalResponseBodyStream = context.Response.Body;
                using var responseBodyStream = _recyclableMemoryStreamManager.GetStream();
                context.Response.Body = responseBodyStream;

                // Call next middleware
                await _next(context);

                // Log response if enabled
                if (_config.HttpLogging.LogResponses)
                {
                    stopwatch.Stop();
                    await LogHttpResponse(context, correlationId, stopwatch.ElapsedMilliseconds);
                }

                // Copy response back to original stream
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                await context.Response.Body.CopyToAsync(originalResponseBodyStream);
                context.Response.Body = originalResponseBodyStream;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogException(ex, correlationId, context.Request.Path);
                throw;
            }
        }

        private async Task LogHttpRequest(HttpContext context, string correlationId)
        {
            try
            {
                var request = context.Request;
                var requestBody = await ReadRequestBody(request);

                var logData = new
                {
                    Method = request.Method,
                    Path = request.Path.Value,
                    QueryString = request.QueryString.Value,
                    Headers = request.Headers.FilterSensitiveHeaders(_config.HttpLogging.SensitiveHeaders, _config.HttpLogging.MaxHeaderSize),
                    Body = requestBody?.MaskSensitiveFields(_config.HttpLogging.SensitiveFields)?.Crop(_config.HttpLogging.MaxBodySize),
                    ContentType = request.ContentType,
                    ContentLength = request.ContentLength,
                    RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = request.Headers["User-Agent"].ToString()
                };

                _appLogger.Log(
                    "{LogType} {Method} {Path} {CorrelationId} {LogData}",
                    LogType.HttpRequest.ToString(),
                    request.Method,
                    request.Path.Value,
                    correlationId,
                    JsonConvert.SerializeObject(logData, Formatting.None)
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging HTTP request: {ex}");
            }
        }

        private async Task LogHttpResponse(HttpContext context, string correlationId, long elapsedMilliseconds)
        {
            try
            {
                var response = context.Response;
                var responseBody = await ReadResponseBody(response);

                var logData = new
                {
                    StatusCode = response.StatusCode,
                    Headers = response.Headers.FilterSensitiveHeaders(_config.HttpLogging.SensitiveHeaders, _config.HttpLogging.MaxHeaderSize),
                    Body = responseBody?.MaskSensitiveFields(_config.HttpLogging.SensitiveFields)?.Crop(_config.HttpLogging.MaxBodySize),
                    ContentType = response.ContentType,
                    ContentLength = response.ContentLength,
                    ElapsedMilliseconds = elapsedMilliseconds
                };

                _appLogger.Log(
                    "{LogType} {StatusCode} {Path} {CorrelationId} {ElapsedMs} {LogData}",
                    LogType.HttpResponse.ToString(),
                    response.StatusCode,
                    context.Request.Path.Value,
                    correlationId,
                    elapsedMilliseconds,
                    JsonConvert.SerializeObject(logData, Formatting.None)
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging HTTP response: {ex}");
            }
        }

        private async Task<string> ReadRequestBody(HttpRequest request)
        {
            if (!request.Body.CanSeek)
            {
                request.EnableBuffering();
            }

            return await request.Body.ReadStreamAsync(_config.HttpLogging.MaxBodySize);
        }

        private async Task<string> ReadResponseBody(HttpResponse response)
        {
            if (response.Body.CanSeek)
            {
                return await response.Body.ReadStreamAsync(_config.HttpLogging.MaxBodySize);
            }
            return string.Empty;
        }

        private void LogException(Exception exception, string correlationId, PathString path)
        {
            try
            {
                var logData = new
                {
                    Path = path.Value,
                    ExceptionType = exception.GetType().Name,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace?.Crop(600 * 1024)
                };

                _appLogger.Log(
                    "{LogType} {ExceptionType} {Path} {CorrelationId} {LogData}",
                    LogType.Exception.ToString(),
                    exception.GetType().Name,
                    path.Value,
                    correlationId,
                    JsonConvert.SerializeObject(logData, Formatting.None)
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging HTTP exception: {ex}");
            }
        }
    }
}