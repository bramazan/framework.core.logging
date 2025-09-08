using Framework.Core.Logging.Helper;
using Framework.Core.Logging.Logging.AppLogger;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection;

namespace Framework.Core.Logging.ActionFilter
{
    public class MethodLoggingActionFilter : ActionFilterAttribute, IExceptionFilter
    {
        private readonly IAppLogger _appLogger;
        private readonly PlatformAppLoggerConfiguration _config;
        private const string StopwatchKey = "MethodLogging_Stopwatch";
        private const string CorrelationIdKey = "MethodLogging_CorrelationId";

        public MethodLoggingActionFilter(IAppLogger appLogger, PlatformAppLoggerConfiguration config)
        {
            _appLogger = appLogger;
            _config = config;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!_config.HttpLogging.LogMethodEntryExit)
            {
                base.OnActionExecuting(context);
                return;
            }

            try
            {
                var stopwatch = Stopwatch.StartNew();
                context.HttpContext.Items[StopwatchKey] = stopwatch;

                var methodBase = GetMethodBase(context);
                var correlationId = LogMethodEntry(context, methodBase);
                
                context.HttpContext.Items[CorrelationIdKey] = correlationId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MethodLoggingActionFilter.OnActionExecuting: {ex}");
            }

            base.OnActionExecuting(context);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (!_config.HttpLogging.LogMethodEntryExit)
            {
                base.OnActionExecuted(context);
                return;
            }

            try
            {
                if (context.HttpContext.Items.TryGetValue(StopwatchKey, out var stopwatchObj) &&
                    stopwatchObj is Stopwatch stopwatch)
                {
                    stopwatch.Stop();

                    var correlationId = context.HttpContext.Items[CorrelationIdKey]?.ToString() ?? "";
                    var methodBase = GetMethodBase(context);

                    LogMethodExit(context, methodBase, stopwatch.ElapsedMilliseconds, correlationId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MethodLoggingActionFilter.OnActionExecuted: {ex}");
            }

            base.OnActionExecuted(context);
        }

        public void OnException(ExceptionContext context)
        {
            if (!_config.HttpLogging.LogMethodEntryExit)
            {
                return;
            }

            try
            {
                var correlationId = context.HttpContext.Items[CorrelationIdKey]?.ToString() ?? "";
                var methodBase = GetMethodBase(context);

                LogMethodException(context, methodBase, correlationId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MethodLoggingActionFilter.OnException: {ex}");
            }
        }

        private string LogMethodEntry(ActionExecutingContext context, MethodBase methodBase)
        {
            try
            {
                var logEventInfo = new
                {
                    ControllerName = context.Controller?.GetType().Name,
                    ActionName = context.ActionDescriptor.DisplayName,
                    Parameters = SerializeActionArguments(context.ActionArguments),
                    HttpMethod = context.HttpContext.Request.Method,
                    Path = context.HttpContext.Request.Path.Value,
                    QueryString = context.HttpContext.Request.QueryString.Value,
                    UserAgent = context.HttpContext.Request.Headers["User-Agent"].ToString(),
                    RemoteIpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString()
                };

                return _appLogger.MethodEntry(logEventInfo, methodBase);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging method entry: {ex}");
                return "";
            }
        }

        private void LogMethodExit(ActionExecutedContext context, MethodBase methodBase, long elapsedMilliseconds, string correlationId)
        {
            try
            {
                var logEventInfo = new
                {
                    ControllerName = context.Controller?.GetType().Name,
                    ActionName = context.ActionDescriptor.DisplayName,
                    StatusCode = context.HttpContext.Response.StatusCode,
                    ElapsedMilliseconds = elapsedMilliseconds,
                    HasException = context.Exception != null,
                    ResultType = context.Result?.GetType().Name,
                    ContentType = context.HttpContext.Response.ContentType
                };

                var messageCode = context.Exception != null ? "ERROR" : "SUCCESS";
                _appLogger.MethodExit(logEventInfo, methodBase, elapsedMilliseconds, messageCode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging method exit: {ex}");
            }
        }

        private void LogMethodException(ExceptionContext context, MethodBase methodBase, string correlationId)
        {
            try
            {
                var controllerName = context.RouteData?.Values["controller"]?.ToString() ?? "Unknown";
                var extraMessage = $"Controller: {controllerName}, Action: {context.ActionDescriptor.DisplayName}";
                _appLogger.Exception(context.Exception, methodBase, extraMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging method exception: {ex}");
            }
        }

        private MethodBase GetMethodBase(FilterContext context)
        {
            try
            {
                if (context is ActionExecutingContext executingContext)
                {
                    return executingContext.ActionDescriptor.GetType().GetMethod("GetMethodInfo")?.Invoke(executingContext.ActionDescriptor, null) as MethodBase
                           ?? MethodBase.GetCurrentMethod() ?? typeof(MethodLoggingActionFilter).GetMethod(nameof(GetMethodBase), BindingFlags.NonPublic | BindingFlags.Instance)!;
                }
                else if (context is ActionExecutedContext executedContext)
                {
                    return executedContext.ActionDescriptor.GetType().GetMethod("GetMethodInfo")?.Invoke(executedContext.ActionDescriptor, null) as MethodBase
                           ?? MethodBase.GetCurrentMethod() ?? typeof(MethodLoggingActionFilter).GetMethod(nameof(GetMethodBase), BindingFlags.NonPublic | BindingFlags.Instance)!;
                }
                else if (context is ExceptionContext exceptionContext)
                {
                    return exceptionContext.ActionDescriptor.GetType().GetMethod("GetMethodInfo")?.Invoke(exceptionContext.ActionDescriptor, null) as MethodBase
                           ?? MethodBase.GetCurrentMethod() ?? typeof(MethodLoggingActionFilter).GetMethod(nameof(GetMethodBase), BindingFlags.NonPublic | BindingFlags.Instance)!;
                }

                return MethodBase.GetCurrentMethod() ?? typeof(MethodLoggingActionFilter).GetMethod(nameof(GetMethodBase), BindingFlags.NonPublic | BindingFlags.Instance)!;
            }
            catch
            {
                return MethodBase.GetCurrentMethod() ?? typeof(MethodLoggingActionFilter).GetMethod(nameof(GetMethodBase), BindingFlags.NonPublic | BindingFlags.Instance)!;
            }
        }

        private string SerializeActionArguments(IDictionary<string, object?> actionArguments)
        {
            try
            {
                if (actionArguments == null || !actionArguments.Any())
                    return "{}";

                var sanitizedArguments = new Dictionary<string, object>();
                
                foreach (var arg in actionArguments)
                {
                    if (arg.Value == null)
                    {
                        sanitizedArguments[arg.Key] = null!;
                        continue;
                    }

                    var serialized = JsonConvert.SerializeObject(arg.Value, Formatting.None);
                    
                    // Apply sensitive field masking
                    var masked = serialized.MaskSensitiveFields(_config.HttpLogging.SensitiveFields);
                    
                    // Try to deserialize back to object for cleaner JSON structure
                    try
                    {
                        sanitizedArguments[arg.Key] = JsonConvert.DeserializeObject(masked) ?? masked;
                    }
                    catch
                    {
                        sanitizedArguments[arg.Key] = masked;
                    }
                }

                var result = JsonConvert.SerializeObject(sanitizedArguments, Formatting.None);
                return result.Length > _config.HttpLogging.MaxBodySize 
                    ? result.Substring(0, _config.HttpLogging.MaxBodySize) + " [CROPPED]"
                    : result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serializing action arguments: {ex}");
                return "{}";
            }
        }
    }
}