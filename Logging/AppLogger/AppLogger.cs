using Framework.Core.Logging.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;
using Microsoft.Extensions.Logging;

namespace Framework.Core.Logging.Logging.AppLogger
{

    public class AppLogger : LoggerBase, IAppLogger
    {

        private readonly PlatformAppLoggerConfiguration _currentConfig;
        private readonly ICorrelationIdHelper _correlationIdHelper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AppLogger> _logger;

        public AppLogger(PlatformAppLoggerConfiguration config,
            ICorrelationIdHelper correlationIdHelper,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AppLogger> logger) : base("AppLogger", config.ConsoleEnabled, config.DebugMode)
        {
            _currentConfig = config;

            AnnounceConfig();

            _correlationIdHelper = correlationIdHelper;
            _httpContextAccessor = httpContextAccessor;

            Trace("Tracelog: PlatformAppLoggerProvider is up and running.", MethodBase.GetCurrentMethod());
            _logger = logger;
        }

        public static AppLogger Create(IConfiguration configuration, ICorrelationIdHelper correlationIdHelper, ILogger<AppLogger> logger, IHttpContextAccessor httpContextAccessor = null)
        {
            return new AppLogger(PlatformAppLoggerConfiguration.Create(configuration), correlationIdHelper, httpContextAccessor ?? new HttpContextAccessor(),logger);
        }

        public static AppLogger Create(IConfiguration configuration, ILogger<AppLogger> logger, IHttpContextAccessor httpContextAccessor = null)
        {
            return new AppLogger(PlatformAppLoggerConfiguration.Create(configuration), new CorrelationIdHelper(httpContextAccessor), httpContextAccessor ?? new HttpContextAccessor(),logger);
        }

        private void AnnounceConfig()
        {
            if (_currentConfig.ConsoleEnabled)
            {
                Console.Out.WriteLine($"PlatformLog: Console logging is enabled");
            }
            else
            {
                Console.Out.WriteLine($"PlatformLog: Console logging is disabled");
            }

            if (_currentConfig.DebugMode)
            {
                Console.Out.WriteLine($"PlatformLog: DebugMode is enabled");
            }
            else
            {
                Console.Out.WriteLine($"PlatformLog: DebugMode is disabled");
            }
        }


#nullable enable
        public void Log(string messageTemplate, params object?[]? propertyValues)
        {
            try
            {
                AppLog appLog = new()
                {
                    Timestamp = DateTimeOffset.Now,
                    Level = "Information",
                    MessageTemplate = messageTemplate,
                    Properties = new()
                };


                var props = messageTemplate.Split(' ');

                if (props.Length != propertyValues?.Length)
                    throw new ArgumentException("Message template is not fit to the log data");

                var index = 0;
                foreach (var prop in props)
                {
                    string key = prop[1..^1];
                    appLog.Properties[key] = propertyValues[index];
                    index++;
                }

                appLog.Properties["MachineName"] = PlatformAppLoggerConfiguration.MachineName;
                appLog.Properties["Environment"] = PlatformAppLoggerConfiguration.EnvironmentName;
                appLog.Properties["LoggerApplicationName"] = _currentConfig.LoggerApplicationName;

                var json = JsonConvert.SerializeObject(appLog, Formatting.None);

                _logger.LogInformation(json);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine($"Error while sending logs to queue: {ex}");
            }
        }
#nullable disable

        //IAppLogger
        public string MethodEntry(object logEventInfo, MethodBase methodBase)
        {
            try
            {
                var _correlationId = _correlationIdHelper.Get();

                Log(
                    "{LogType} {ClassName} {AssemblyName} {MethodName} {CorrelationId} {LogData} {LogHeader}",
                    LogType.MethodEntry.ToString(),
                    methodBase.DeclaringType?.Name,
                    methodBase?.DeclaringType?.Assembly.GetName().Name,
                    methodBase?.Name,
                    _correlationId,
                    JsonConvert.SerializeObject(logEventInfo, Formatting.None).ClearSensitiveValues().Crop(180 * 1024),
                    _httpContextAccessor?.HttpContext?.Request?.Headers.GetHeaderJson(20 * 1024).ClearSensitiveValues());

                return _correlationId;
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("Error while PlatformLog MethodEntry: " + ex.ToString());
                return null;
            }
        }

        public string MethodExit(object logEventInfo, MethodBase methodBase, double methodExecutionDuration, string messageCode)
        {
            try
            {
                var _correlationId = _correlationIdHelper.Get();

                Log(
                    "{LogType} {ClassName} {AssemblyName} {MethodName} {CorrelationId} {MethodExecutionDuration} {MessageCode} {LogData} {LogHeader}",
                    LogType.MethodExit.ToString(),
                    methodBase.DeclaringType?.Name,
                    methodBase?.DeclaringType?.Assembly.GetName().Name,
                    methodBase?.Name,
                    _correlationId,
                    methodExecutionDuration,
                    messageCode,
                    JsonConvert.SerializeObject(logEventInfo, Formatting.None).ClearSensitiveValues().Crop(180 * 1024),
                    _httpContextAccessor?.HttpContext?.Request?.Headers.GetHeaderJson(20 * 1024).ClearSensitiveValues());

                return _correlationId;
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("Error while PlatformLog MethodExit: " + ex.ToString());
                return null;
            }
        }

        public string Trace(string message, MethodBase methodBase)
        {
            try
            {
                var _correlationId = _correlationIdHelper.Get();

                Log(
                    "{LogType} {ClassName} {AssemblyName} {MethodName} {CorrelationId} {LogData} {LogHeader}",
                    LogType.Trace.ToString(),
                    methodBase.DeclaringType?.Name,
                    methodBase?.DeclaringType?.Assembly.GetName().Name,
                    methodBase?.Name,
                    _correlationId,
                    message.ClearSensitiveValues().Crop(180 * 1024),
                    _httpContextAccessor?.HttpContext?.Request?.Headers.GetHeaderJson(20 * 1024).ClearSensitiveValues());

                return _correlationId;
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("Error while PlatformLog Trace: " + ex.ToString());
                return null;
            }
        }

        public string Exception(Exception exception, MethodBase methodBase, string extraMessage = "")
        {
            try
            {
                var isCustomException = exception?.GetType().Name?.Contains("BusinessRuleException");
                var _correlationId = _correlationIdHelper.Get();

                Log(
                    "{LogType} {ClassName} {AssemblyName} {MethodName} {CorrelationId} {SingleLineException} {Message} {ExtraMessage} {IsCustomException} {LogHeader}",
                    LogType.Exception.ToString(),
                    methodBase?.DeclaringType?.Name,
                    methodBase?.DeclaringType?.Assembly?.GetName().Name,
                    methodBase?.Name,
                    _correlationId,
                    exception?.ToString().ClearSensitiveValues().Crop(600 * 1024),
                    exception?.Message.ClearSensitiveValues().Crop(100 * 1024),
                    extraMessage?.ClearSensitiveValues().Crop(200 * 1024),
                    isCustomException,
                    _httpContextAccessor?.HttpContext?.Request?.Headers.GetHeaderJson(20 * 1024).ClearSensitiveValues());

                return _correlationId;
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("Error while PlatformLog Exception: " + ex.ToString());
                return null;
            }
        }
    }
}

