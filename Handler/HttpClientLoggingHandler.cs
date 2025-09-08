using Framework.Core.Logging.Helper;
using Framework.Core.Logging.Logging.AppLogger;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.Core.Logging.Handler
{
    public class HttpClientLoggingHandler : DelegatingHandler
    {
        private readonly IAppLogger _appLogger;
        private readonly PlatformAppLoggerConfiguration _config;
        private static readonly ICorrelationIdHelper _correlationIdHelper = new CorrelationIdHelper();

        public HttpClientLoggingHandler(IAppLogger appLogger, PlatformAppLoggerConfiguration config)
        {
            _appLogger = appLogger;
            _config = config;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!_config.HttpLogging.LogHttpClient)
            {
                return await base.SendAsync(request, cancellationToken);
            }

            var stopwatch = Stopwatch.StartNew();
            var correlationId = _correlationIdHelper.Get();
            
            try
            {
                // Log outgoing request
                await LogHttpClientRequest(request, correlationId);

                // Send request
                var response = await base.SendAsync(request, cancellationToken);

                // Log response
                stopwatch.Stop();
                await LogHttpClientResponse(request, response, correlationId, stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogHttpClientException(request, ex, correlationId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private async Task LogHttpClientRequest(HttpRequestMessage request, string correlationId)
        {
            try
            {
                var requestBody = string.Empty;
                if (request.Content != null)
                {
                    var contentBytes = await request.Content.ReadAsByteArrayAsync();
                    if (contentBytes.Length > 0)
                    {
                        var maxLength = Math.Min(contentBytes.Length, _config.HttpLogging.MaxBodySize);
                        requestBody = Encoding.UTF8.GetString(contentBytes, 0, maxLength);
                        if (contentBytes.Length > maxLength)
                        {
                            requestBody += " [CROPPED]";
                        }
                    }
                }

                var logData = new
                {
                    Method = request.Method.Method,
                    Uri = request.RequestUri?.ToString(),
                    Headers = SerializeHeaders(request.Headers),
                    ContentHeaders = request.Content != null ? SerializeHeaders(request.Content.Headers) : "{}",
                    Body = requestBody?.MaskSensitiveFields(_config.HttpLogging.SensitiveFields)?.ClearSensitiveValues(),
                    ContentType = request.Content?.Headers?.ContentType?.ToString(),
                    ContentLength = request.Content?.Headers?.ContentLength
                };

                _appLogger.Log(
                    "{LogType} {Method} {Uri} {CorrelationId} {LogData}",
                    LogType.HttpClientRequest.ToString(),
                    request.Method.Method,
                    request.RequestUri?.ToString(),
                    correlationId,
                    JsonConvert.SerializeObject(logData, Formatting.None)
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging HTTP client request: {ex}");
            }
        }

        private async Task LogHttpClientResponse(HttpRequestMessage request, HttpResponseMessage response, string correlationId, long elapsedMilliseconds)
        {
            try
            {
                var responseBody = string.Empty;
                if (response.Content != null)
                {
                    var contentBytes = await response.Content.ReadAsByteArrayAsync();
                    if (contentBytes.Length > 0)
                    {
                        var maxLength = Math.Min(contentBytes.Length, _config.HttpLogging.MaxBodySize);
                        responseBody = Encoding.UTF8.GetString(contentBytes, 0, maxLength);
                        if (contentBytes.Length > maxLength)
                        {
                            responseBody += " [CROPPED]";
                        }
                    }
                }

                var logData = new
                {
                    StatusCode = (int)response.StatusCode,
                    ReasonPhrase = response.ReasonPhrase,
                    Headers = SerializeHeaders(response.Headers),
                    ContentHeaders = response.Content != null ? SerializeHeaders(response.Content.Headers) : "{}",
                    Body = responseBody?.MaskSensitiveFields(_config.HttpLogging.SensitiveFields)?.ClearSensitiveValues(),
                    ContentType = response.Content?.Headers?.ContentType?.ToString(),
                    ContentLength = response.Content?.Headers?.ContentLength,
                    ElapsedMilliseconds = elapsedMilliseconds,
                    IsSuccessStatusCode = response.IsSuccessStatusCode
                };

                _appLogger.Log(
                    "{LogType} {StatusCode} {Uri} {CorrelationId} {ElapsedMs} {LogData}",
                    LogType.HttpClientResponse.ToString(),
                    (int)response.StatusCode,
                    request.RequestUri?.ToString(),
                    correlationId,
                    elapsedMilliseconds,
                    JsonConvert.SerializeObject(logData, Formatting.None)
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging HTTP client response: {ex}");
            }
        }

        private void LogHttpClientException(HttpRequestMessage request, Exception exception, string correlationId, long elapsedMilliseconds)
        {
            try
            {
                var logData = new
                {
                    Uri = request.RequestUri?.ToString(),
                    Method = request.Method.Method,
                    ExceptionType = exception.GetType().Name,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace?.Crop(600 * 1024),
                    ElapsedMilliseconds = elapsedMilliseconds
                };

                _appLogger.Log(
                    "{LogType} {ExceptionType} {Uri} {CorrelationId} {ElapsedMs} {LogData}",
                    LogType.Exception.ToString(),
                    exception.GetType().Name,
                    request.RequestUri?.ToString(),
                    correlationId,
                    elapsedMilliseconds,
                    JsonConvert.SerializeObject(logData, Formatting.None)
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging HTTP client exception: {ex}");
            }
        }

        private string SerializeHeaders(System.Net.Http.Headers.HttpHeaders headers)
        {
            try
            {
                var headerDict = new Dictionary<string, string>();
                
                foreach (var header in headers)
                {
                    var isSensitive = _config.HttpLogging.SensitiveHeaders?.Any(s => 
                        string.Equals(s, header.Key, StringComparison.OrdinalIgnoreCase)) == true;
                    
                    if (isSensitive)
                    {
                        headerDict[header.Key] = "***";
                    }
                    else
                    {
                        headerDict[header.Key] = string.Join(", ", header.Value);
                    }
                }

                var json = JsonConvert.SerializeObject(headerDict, Formatting.None);
                return json.Length > _config.HttpLogging.MaxHeaderSize 
                    ? json.Crop(_config.HttpLogging.MaxHeaderSize) 
                    : json;
            }
            catch
            {
                return "{}";
            }
        }
    }
}